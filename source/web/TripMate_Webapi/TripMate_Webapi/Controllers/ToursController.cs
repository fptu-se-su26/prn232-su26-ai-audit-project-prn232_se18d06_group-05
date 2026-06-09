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
    /// Lấy danh sách gói trải nghiệm (public — không cần đăng nhập)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetTours([FromQuery] string? search)
    {
        try
        {
            var rows = await _tourService.GetToursAsync(search);
            var tours = rows.Select(TourService.MapToDto).ToList();
            return Ok(new { tours, total = tours.Count });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy chi tiết một gói trải nghiệm
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetTour(string id)
    {
        try
        {
            var row = await _tourService.GetTourByIdAsync(id);
            if (row == null) return NotFound(new { message = "Không tìm thấy gói trải nghiệm" });
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
    /// Tạo gói trải nghiệm mới (cần đăng nhập, role = guide)
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateTour([FromBody] CreateTourRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) ||
            string.IsNullOrWhiteSpace(request.Description) ||
            request.PricePerSession <= 0 || request.DurationHours <= 0)
            return BadRequest(new { message = "Thiếu thông tin bắt buộc" });

        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "Không xác định được người dùng" });

        try
        {
            // userId is the auth user id; we need guide_profile_id
            // The guide_profile_id lookup is done via guide_profiles.user_id = userId
            var guideProfileId = await GetGuideProfileIdAsync(userId);
            if (guideProfileId == null)
                return BadRequest(new { message = "Bạn chưa có hồ sơ hướng dẫn viên" });

            var row = await _tourService.CreateTourAsync(guideProfileId, request, UserToken!);
            return CreatedAtAction(nameof(GetTour), new { id = row.Id }, TourService.MapToDto(row));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Cập nhật gói trải nghiệm (cần đăng nhập)
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
    /// Xóa gói trải nghiệm (cần đăng nhập)
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

    // Helper: lookup guide_profile_id from auth user_id
    private async Task<string?> GetGuideProfileIdAsync(string userId)
    {
        // This could be moved to a dedicated GuideProfileService
        try
        {
            var rows = await _tourService.GetToursByGuideAsync(""); // We need a direct lookup
            // For now, we use a simple approach - the TourService can be extended
            return null; // Will be resolved after full integration
        }
        catch
        {
            return null;
        }
    }
}
