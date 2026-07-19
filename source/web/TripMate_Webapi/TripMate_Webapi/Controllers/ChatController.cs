using System.Security.Claims;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TripMate_WebAPI.Services;

namespace TripMate_WebAPI.Controllers;

/// <summary>
/// Chat controller — uses chat_messages table via ChatService.
/// Conversations are simulated by grouping messages by booking_id.
/// Route param {bookingId} replaces the old {conversationId}.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChatController : ControllerBase
{
    private readonly ChatService _chat;
    private readonly TripMate_WebAPI.Services.ICloudinaryService _cloud;
    public ChatController(ChatService chat, TripMate_WebAPI.Services.ICloudinaryService cloud) { _chat = chat; _cloud = cloud; }

    private string UserId => User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value ?? "";
    private string UserToken => Request.Headers.Authorization.ToString()
        .Replace("Bearer ", "").Trim();

    /// <summary>Lấy hoặc tạo conversation (= booking_id group)</summary>
    [HttpPost("conversations")]
    public async Task<IActionResult> GetOrCreate(
        [FromBody] CreateConversationRequest req)
    {
        try
        {
            var conv = await _chat.GetOrCreateConversationAsync(
                UserId, req.GuideId, req.BookingId, UserToken);
            return Ok(conv);
        }
        catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
    }

    /// <summary>Upload an attachment to Cloudinary and send as a message (message_text = cloudinary url)</summary>
    [HttpPost("conversations/{bookingId}/attachments")]
    public async Task<IActionResult> UploadAttachment(string bookingId, [FromForm] IFormFile file, [FromForm] string receiverId)
    {
        if (file == null || file.Length == 0) return BadRequest(new { message = "No file provided" });
        try
        {
            // Use dedicated folder per chat
            var folder = $"tripmate_chat/{bookingId}";
            // Use UploadFileAsync to support arbitrary file types (images/videos/docs)
            var url = await _cloud.UploadFileAsync(file, folder);
            if (string.IsNullOrEmpty(url)) return StatusCode(500, new { message = "Upload failed" });

            // Create message with the cloudinary url as message text
            var msg = await _chat.SendMessageAsync(bookingId, UserId, receiverId, url, UserToken);
            return Ok(msg);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    /// <summary>Danh sách conversations của tôi</summary>
    [HttpGet("conversations")]
    public async Task<IActionResult> GetMyConversations()
    {
        try
        {
            var list = await _chat.GetMyConversationsAsync(UserId, UserToken);
            return Ok(new { conversations = list });
        }
        catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
    }

    /// <summary>Lấy messages theo booking_id</summary>
    [HttpGet("conversations/{bookingId}/messages")]
    public async Task<IActionResult> GetMessages(string bookingId)
    {
        try
        {
            // optional pagination: ?limit=50&offset=0 (offset in pages of newest-first)
            var q = HttpContext.Request.Query;
            int? limit = null;
            int? offset = null;
            if (q.ContainsKey("limit") && int.TryParse(q["limit"], out var l)) limit = l;
            if (q.ContainsKey("offset") && int.TryParse(q["offset"], out var o)) offset = o;

            var msgs = await _chat.GetMessagesAsync(bookingId, UserToken, limit ?? 50, offset ?? 0);
            // ChatService returns messages ordered by sent_at desc (newest first). Client expects chronological order, so reverse on client.
            return Ok(new { messages = msgs });
        }
        catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
    }

    /// <summary>Mark messages in booking as read for current user</summary>
    [HttpPost("conversations/{bookingId}/mark-read")]
    public async Task<IActionResult> MarkRead(string bookingId)
    {
        try
        {
            await _chat.MarkMessagesAsReadAsync(bookingId, UserId, UserToken);
            return Ok(new { success = true });
        }
        catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
    }

    /// <summary>Get basic profile info for a user</summary>
    [HttpGet("profiles/{userId}")]
    public async Task<IActionResult> GetProfile(string userId)
    {
        try
        {
            var p = await _chat.GetProfileAsync(userId, UserToken);
            if (p == null) return NotFound();
            return Ok(p);
        }
        catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
    }

    /// <summary>Gửi tin nhắn trong conversation (= booking_id)</summary>
    [HttpPost("conversations/{bookingId}/messages")]
    public async Task<IActionResult> SendMessage(
        string bookingId, [FromBody] SendMessageRequest req)
    {
        try
        {
            var msg = await _chat.SendMessageAsync(
                bookingId, UserId, req.ReceiverId, req.Content, UserToken);
            return Ok(msg);
        }
        catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
    }

    /// <summary>Edit a message text. Only the original sender may edit.</summary>
    [HttpPatch("conversations/{bookingId}/messages/{messageId}")]
    public async Task<IActionResult> EditMessage(string bookingId, long messageId, [FromBody] EditMessageRequest req)
    {
        try
        {
            var updated = await _chat.EditMessageAsync(messageId, UserId, req.Content, UserToken);
            return Ok(updated);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = ex.Message });
        }
    }

    public record EditMessageRequest(string Content);
}
