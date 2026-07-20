using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripMate_WebAPI.DTOs.Notification;
using TripMate_WebAPI.Services;

namespace TripMate_WebAPI.Controllers;

[ApiController]
[Authorize]
[Route("api/notifications")]
public sealed class NotificationsController : ControllerBase
{
    private readonly INotificationService _notifications;

    public NotificationsController(INotificationService notifications)
    {
        _notifications = notifications;
    }

    private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier)
                              ?? User.FindFirstValue("sub");

    private string UserToken => Request.Headers.Authorization.ToString() is { Length: > 0 } header
        ? header
        : Request.Cookies["access_token"] ?? string.Empty;

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] int limit = 30,
        [FromQuery] string? before = null,
        [FromQuery] bool unreadOnly = false)
    {
        if (string.IsNullOrWhiteSpace(UserId) || string.IsNullOrWhiteSpace(UserToken)) return Unauthorized();
        var page = await _notifications.GetForUserAsync(UserId, UserToken, limit, before, unreadOnly);
        return Ok(page);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        if (string.IsNullOrWhiteSpace(UserId) || string.IsNullOrWhiteSpace(UserToken)) return Unauthorized();
        return Ok(new { count = await _notifications.GetUnreadCountAsync(UserId, UserToken) });
    }

    [HttpPatch("{id}/read")]
    public async Task<IActionResult> MarkRead(string id)
    {
        if (string.IsNullOrWhiteSpace(UserId) || string.IsNullOrWhiteSpace(UserToken)) return Unauthorized();
        return await _notifications.MarkAsReadAsync(id, UserId, UserToken)
            ? Ok(new { success = true })
            : NotFound(new { message = "Notification not found" });
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        if (string.IsNullOrWhiteSpace(UserId) || string.IsNullOrWhiteSpace(UserToken)) return Unauthorized();
        var updated = await _notifications.MarkAllAsReadAsync(UserId, UserToken);
        return updated
            ? Ok(new { success = true })
            : StatusCode(StatusCodes.Status502BadGateway, new
            {
                success = false,
                message = "Supabase did not accept the mark-all-as-read update"
            });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        if (string.IsNullOrWhiteSpace(UserId) || string.IsNullOrWhiteSpace(UserToken)) return Unauthorized();
        return await _notifications.DeleteAsync(id, UserId, UserToken)
            ? NoContent()
            : NotFound(new { message = "Notification not found" });
    }

    [HttpPost("admin/send")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> SendToUser([FromBody] NotificationSendRequest request)
    {
        if (!NotificationTypes.AdminSendable.Contains(request.Type))
            return BadRequest(new { message = "This notification type cannot be sent manually" });

        await _notifications.SendAsync(
            request.UserId,
            request.Type,
            request.Title,
            request.Message,
            request.Data,
            request.ActionUrl,
            request.DedupeKey,
            request.SendEmail);
        return Accepted(new { success = true });
    }

    [HttpPost("admin/announce")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> Announce([FromBody] SystemAnnouncementRequest request)
    {
        var role = string.IsNullOrWhiteSpace(request.Role) ? null : request.Role.Trim().ToLowerInvariant();
        if (role is not null && role is not ("traveler" or "guide" or "admin"))
            return BadRequest(new { message = "Role must be traveler, guide, admin, or empty for everyone" });

        await _notifications.SendToRoleAsync(
            role,
            NotificationTypes.SystemAnnouncement,
            request.Title,
            request.Message,
            actionUrl: request.ActionUrl,
            dedupeKey: $"announcement:{Guid.NewGuid()}",
            sendEmail: request.SendEmail);
        return Accepted(new { success = true });
    }

    [HttpPost("admin/support-ticket-update")]
    [Authorize(Roles = "admin")]
    public async Task<IActionResult> SupportTicketUpdate([FromBody] SupportTicketUpdateRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.UserId) || string.IsNullOrWhiteSpace(request.TicketId))
            return BadRequest(new { message = "UserId and TicketId are required" });

        await _notifications.SendAsync(
            request.UserId,
            NotificationTypes.SupportTicketUpdated,
            $"Support ticket {request.Status}",
            request.Message,
            new { ticketId = request.TicketId, status = request.Status },
            "/Guide/Support",
            $"support-ticket:{request.TicketId}:{request.Status}",
            request.SendEmail);
        return Accepted(new { success = true });
    }
}
