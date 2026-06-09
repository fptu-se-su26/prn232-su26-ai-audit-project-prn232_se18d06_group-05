using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
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
    public ChatController(ChatService chat) => _chat = chat;

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
            var msgs = await _chat.GetMessagesAsync(bookingId, UserToken);
            return Ok(new { messages = msgs });
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
}
