using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using TripMate_WebAPI.Services;

namespace TripMate_WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ToursController : ControllerBase
{
    private readonly TourService _tourService;

    public ToursController(TourService tourService)
    {
        _tourService = tourService;
    }

    /// <summary>
    /// Lấy danh sách tour (public — không cần đăng nhập)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetTours(
        [FromQuery] string? location,
        [FromQuery] string? search)
    {
        try
        {
            var rows = await _tourService.GetToursAsync(location, search);
            var tours = rows.Select(TourService.MapToDto).ToList();
            return Ok(new { tours, total = tours.Count });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy chi tiết một tour
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetTour(string id)
    {
        try
        {
            var row = await _tourService.GetTourByIdAsync(id);
            if (row == null) return NotFound(new { message = "Không tìm thấy tour" });
            return Ok(TourService.MapToDto(row));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    private string? UserToken =>
        Request.Headers.Authorization.ToString().Replace("Bearer ", "").Trim()
        is { Length: > 0 } t ? t : null;

    /// <summary>
    /// Tạo tour mới (cần đăng nhập, role = guide)
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateTour([FromBody] CreateTourRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) ||
            string.IsNullOrWhiteSpace(request.Location) ||
            request.Price <= 0 || request.DurationHours <= 0)
            return BadRequest(new { message = "Thiếu thông tin bắt buộc" });

        var guideId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(guideId))
            return Unauthorized(new { message = "Không xác định được người dùng" });

        try
        {
            var row = await _tourService.CreateTourAsync(guideId, request, UserToken!);
            return CreatedAtAction(nameof(GetTour), new { id = row.Id }, TourService.MapToDto(row));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Cập nhật tour (cần đăng nhập)
    /// </summary>
    [HttpPatch("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateTour(string id, [FromBody] UpdateTourRequest request)
    {
        try
        {
            var row = await _tourService.UpdateTourAsync(id, request, UserToken!);
            return Ok(TourService.MapToDto(row));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Xóa tour (cần đăng nhập)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteTour(string id)
    {
        try
        {
            await _tourService.DeleteTourAsync(id, UserToken!);
            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
}
