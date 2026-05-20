using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TripMate_WebAPI.Services;

namespace TripMate_WebAPI.Controllers;

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

    /// <summary>Lấy hoặc tạo conversation</summary>
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

    /// <summary>Lấy messages của conversation</summary>
    [HttpGet("conversations/{id}/messages")]
    public async Task<IActionResult> GetMessages(string id)
    {
        try
        {
            var msgs = await _chat.GetMessagesAsync(id, UserToken);
            return Ok(new { messages = msgs });
        }
        catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
    }

    /// <summary>Gửi tin nhắn</summary>
    [HttpPost("conversations/{id}/messages")]
    public async Task<IActionResult> SendMessage(
        string id, [FromBody] SendMessageRequest req)
    {
        try
        {
            var msg = await _chat.SendMessageAsync(id, UserId, req.Content, UserToken);
            return Ok(msg);
        }
        catch (Exception ex) { return StatusCode(500, new { message = ex.Message }); }
    }
}

public record CreateConversationRequest(string GuideId, string BookingId);
public record SendMessageRequest(string Content);
