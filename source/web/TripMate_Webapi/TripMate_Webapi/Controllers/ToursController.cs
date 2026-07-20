using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using TripMate_WebAPI.Services;
using TripMate_Webapi.Entities;

namespace TripMate_WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ToursController : ControllerBase
{
    private readonly TourService _tourService;
    private readonly Supabase.Client _supabase;

    public ToursController(TourService tourService, Supabase.Client supabase)
    {
        _tourService = tourService;
        _supabase = supabase;
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
    /// Lấy danh sách gói trải nghiệm của chính hướng dẫn viên đang đăng nhập
    /// </summary>
    [HttpGet("my")]
    [Authorize]
    public async Task<IActionResult> GetMyTours()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "Không xác định được người dùng" });

        try
        {
            var guideProfileId = await GetGuideProfileIdAsync(userId);
            if (guideProfileId == null)
                return BadRequest(new { message = "Bạn chưa có hồ sơ hướng dẫn viên" });

            var rows = await _tourService.GetToursByGuideAsync(guideProfileId);
            var tours = rows.Select(TourService.MapToDto).ToList();
            return Ok(tours);
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
            // 1. Fetch bookings associated with this experience package
            var bookingsRes = await _supabase.From<BookingEntity>()
                .Where(b => b.ExperiencePackageId == id)
                .Get();

            var bookings = bookingsRes.Models;

            // 2. Check for active bookings (Pending=0 or Confirmed=1)
            var activeBookings = bookings.Where(b => b.Status == 0 || b.Status == 1).ToList();
            if (activeBookings.Any())
            {
                return BadRequest(new { message = "Không thể xóa gói trải nghiệm này vì đang có các lịch hẹn hoặc giao dịch chưa hoàn tất với du khách." });
            }

            // 3. If there are historical bookings (Completed=2 or Cancelled=3), we must soft-delete (is_active = false)
            // to preserve booking history and avoid cascade deleting user records.
            if (bookings.Any())
            {
                var req = new UpdateTourRequest { IsActive = false };
                await _tourService.UpdateTourAsync(id, req, UserToken!);
                return Ok(new { message = "Gói trải nghiệm đã được ẩn và đưa vào lưu trữ để bảo toàn lịch sử giao dịch." });
            }

            // 4. If no bookings at all, safe to perform hard delete
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
        try
        {
            if (string.IsNullOrEmpty(UserToken)) return null;
            return await _tourService.GetGuideProfileIdByUserIdAsync(userId, UserToken);
        }
        catch
        {
            return null;
        }
    }
}
