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
    /// Body: { tourAvailabilityId, guests, note? }
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
    {
        if (UserId == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.TourAvailabilityId))
            return BadRequest(new { message = "TourAvailabilityId không được để trống" });

        if (request.Guests < 1)
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
    /// Lấy danh sách ngày trống của một guide_tour
    /// GET /api/bookings/availability/{guideTourId}
    /// </summary>
    [HttpGet("availability/{guideTourId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAvailability(string guideTourId)
    {
        try
        {
            var availability = await _bookingService.GetAvailabilityByGuideTourAsync(guideTourId);
            return Ok(new { availability, total = availability.Count });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }
}
