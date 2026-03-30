using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripMate_WebAPI.Models;
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

    /// <summary>Tạo booking mới</summary>
    [HttpPost]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingRequest request)
    {
        if (UserId == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(request.TourId))
            return BadRequest(new { message = "Tour ID không được để trống" });

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

    /// <summary>Lấy danh sách booking của tôi</summary>
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

    /// <summary>Lấy chi tiết một booking</summary>
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

    /// <summary>Hủy booking</summary>
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
}
