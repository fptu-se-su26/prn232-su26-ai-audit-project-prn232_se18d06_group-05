using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using TripMate_WebAPI.Services;

namespace TripMate_WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly BookingService _bookingService;

    public BookingsController(BookingService bookingService)
    {
        _bookingService = bookingService;
    }

    private string? UserId =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("sub")?.Value;

    private string? UserToken =>
        Request.Headers.Authorization.ToString().Replace("Bearer ", "").Trim()
        is { Length: > 0 } t ? t : null;

    /// <summary>
    /// Tạo booking mới
    /// POST /api/bookings
    /// Body: { experiencePackageId, bookingDate, startTime, guestCount, travelerNotes? }
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
    {
        if (UserId == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.ExperiencePackageId))
            return BadRequest(new { message = "ExperiencePackageId không được để trống" });

        if (string.IsNullOrWhiteSpace(request.BookingDate))
            return BadRequest(new { message = "BookingDate không được để trống" });

        if (string.IsNullOrWhiteSpace(request.StartTime))
            return BadRequest(new { message = "StartTime không được để trống" });

        if (request.GuestCount < 1)
            return BadRequest(new { message = "Số khách phải ít nhất là 1" });

        try
        {
            var booking = await _bookingService.CreateBookingAsync(UserId, request, UserToken!);
            return CreatedAtAction(nameof(GetBooking), new { id = booking.Id }, booking);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy danh sách booking của tôi
    /// GET /api/bookings/my
    /// </summary>
    [HttpGet("my")]
    public async Task<IActionResult> GetMyBookings()
    {
        if (UserId == null) return Unauthorized();

        try
        {
            var bookings = await _bookingService.GetMyBookingsAsync(UserId);
            return Ok(new { bookings, total = bookings.Count });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy chi tiết một booking
    /// GET /api/bookings/{id}
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetBooking(string id)
    {
        try
        {
            var booking = await _bookingService.GetBookingByIdAsync(id);
            if (booking == null) return NotFound(new { message = "Không tìm thấy booking" });
            return Ok(booking);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Hủy booking
    /// DELETE /api/bookings/{id}
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> CancelBooking(string id)
    {
        if (UserId == null) return Unauthorized();

        try
        {
            await _bookingService.CancelBookingAsync(id, UserId);
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Lấy ngày nghỉ/bận của guide (blacklist)
    /// GET /api/bookings/availability/{guideProfileId}
    /// </summary>
    [HttpGet("availability/{guideProfileId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAvailability(string guideProfileId)
    {
        try
        {
            var unavailableDates = await _bookingService.GetGuideAvailabilityAsync(guideProfileId);
            return Ok(new { unavailableDates, total = unavailableDates.Count });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
}
