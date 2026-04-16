using Microsoft.AspNetCore.Mvc;
using TripMate_WebAPI.Services;

namespace TripMate_WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DebugController : ControllerBase
{
    private readonly ChatService _chat;
    private readonly IConfiguration _config;

    public DebugController(ChatService chat, IConfiguration config)
    {
        _chat = chat;
        _config = config;
    }

    /// <summary>Test database connection</summary>
    [HttpGet("db-test")]
    public async Task<IActionResult> TestDatabase()
    {
        try
        {
            var supabaseUrl = _config["Supabase:Url"];
            var anonKey = _config["Supabase:AnonKey"];
            
            if (string.IsNullOrEmpty(supabaseUrl) || string.IsNullOrEmpty(anonKey))
            {
                return BadRequest(new { 
                    error = "Supabase configuration missing",
                    url = string.IsNullOrEmpty(supabaseUrl) ? "missing" : "present",
                    key = string.IsNullOrEmpty(anonKey) ? "missing" : "present"
                });
            }

            // Test simple query
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("apikey", anonKey);
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {anonKey}");
            
            var response = await client.GetAsync($"{supabaseUrl}/rest/v1/conversations?limit=1");
            var content = await response.Content.ReadAsStringAsync();
            
            return Ok(new {
                status = response.IsSuccessStatusCode ? "success" : "error",
                statusCode = (int)response.StatusCode,
                content = content,
                supabaseUrl = supabaseUrl,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { 
                error = ex.Message,
                type = ex.GetType().Name,
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>Test chat service</summary>
    [HttpGet("chat-test")]
    public async Task<IActionResult> TestChatService()
    {
        try
        {
            // Test getting conversations (should work even if empty)
            var conversations = await _chat.GetMyConversationsAsync("test-user-id", "test-token");
            
            return Ok(new {
                status = "success",
                conversationCount = conversations.Count,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { 
                error = ex.Message,
                type = ex.GetType().Name,
                stackTrace = ex.StackTrace,
                timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>Get environment info</summary>
    [HttpGet("env")]
    public IActionResult GetEnvironment()
    {
        return Ok(new {
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            supabaseConfigured = !string.IsNullOrEmpty(_config["Supabase:Url"]),
            timestamp = DateTime.UtcNow
        });
    }
}