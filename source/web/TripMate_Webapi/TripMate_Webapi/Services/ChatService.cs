using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TripMate_WebAPI.Services;

public class ChatService
{
    private readonly HttpClient _http;
    private readonly string _supabaseUrl;
    private readonly string _anonKey;

    private static readonly JsonSerializerOptions _json = new()
    { PropertyNameCaseInsensitive = true };

    public ChatService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _supabaseUrl = config["Supabase:Url"]!;
        _anonKey = config["Supabase:AnonKey"]!;
    }

    // ── Get or create conversation ────────────────────────────────────────────

    public async Task<ConversationDto> GetOrCreateConversationAsync(
        string travelerId, string guideId, string bookingId, string userToken)
    {
        // Check existing
        var url = $"{_supabaseUrl}/rest/v1/conversations" +
                  $"?traveler_id=eq.{travelerId}&guide_id=eq.{guideId}" +
                  $"&booking_id=eq.{bookingId}&select=*";

        var existing = await GetAsync<List<ConvRow>>(url, userToken);
        if (existing?.Count > 0)
            return MapConv(existing[0]);

        // Create new
        var body = new { traveler_id = travelerId, guide_id = guideId, booking_id = bookingId };
        var req = BuildRequest(HttpMethod.Post, $"{_supabaseUrl}/rest/v1/conversations", userToken);
        req.Headers.Add("Prefer", "return=representation");
        req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var res = await _http.SendAsync(req);
        var content = await res.Content.ReadAsStringAsync();
        EnsureSuccess(res, content);

        var rows = JsonSerializer.Deserialize<List<ConvRow>>(content, _json);
        return MapConv(rows!.First());
    }

    // ── Get my conversations ──────────────────────────────────────────────────

    public async Task<List<ConversationDto>> GetMyConversationsAsync(string userId, string userToken)
    {
        var url = $"{_supabaseUrl}/rest/v1/conversations" +
                  $"?or=(traveler_id.eq.{userId},guide_id.eq.{userId})" +
                  $"&order=created_at.desc&select=*";

        var rows = await GetAsync<List<ConvRow>>(url, userToken) ?? [];
        return rows.Select(MapConv).ToList();
    }

    // ── Get messages ──────────────────────────────────────────────────────────

    public async Task<List<MessageDto>> GetMessagesAsync(
        string conversationId, string userToken)
    {
        var url = $"{_supabaseUrl}/rest/v1/messages" +
                  $"?conversation_id=eq.{conversationId}&order=created_at.asc&select=*";

        var rows = await GetAsync<List<MsgRow>>(url, userToken) ?? [];
        return rows.Select(r => new MessageDto(
            r.Id ?? "", r.ConversationId ?? "", r.SenderId ?? "",
            r.Content ?? "", r.IsRead, r.CreatedAt)).ToList();
    }

    // ── Send message ──────────────────────────────────────────────────────────

    public async Task<MessageDto> SendMessageAsync(
        string conversationId, string senderId, string content, string userToken)
    {
        var body = new { conversation_id = conversationId, sender_id = senderId, content };
        var req = BuildRequest(HttpMethod.Post, $"{_supabaseUrl}/rest/v1/messages", userToken);
        req.Headers.Add("Prefer", "return=representation");
        req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var res = await _http.SendAsync(req);
        var c = await res.Content.ReadAsStringAsync();
        EnsureSuccess(res, c);

        var rows = JsonSerializer.Deserialize<List<MsgRow>>(c, _json);
        var row = rows!.First();
        return new MessageDto(row.Id ?? "", row.ConversationId ?? "", row.SenderId ?? "",
            row.Content ?? "", row.IsRead, row.CreatedAt);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<T?> GetAsync<T>(string url, string? token = null)
    {
        var req = BuildRequest(HttpMethod.Get, url, token);
        var res = await _http.SendAsync(req);
        var c = await res.Content.ReadAsStringAsync();
        EnsureSuccess(res, c);
        return JsonSerializer.Deserialize<T>(c, _json);
    }

    private HttpRequestMessage BuildRequest(HttpMethod method, string url, string? token = null)
    {
        var req = new HttpRequestMessage(method, url);
        req.Headers.Add("apikey", _anonKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token ?? _anonKey);
        req.Headers.Add("Accept", "application/json");
        return req;
    }

    private static void EnsureSuccess(HttpResponseMessage r, string c)
    {
        if (!r.IsSuccessStatusCode) throw new Exception($"Supabase {r.StatusCode}: {c}");
    }

    private static ConversationDto MapConv(ConvRow r) =>
        new(r.Id ?? "", r.TravelerId ?? "", r.GuideId ?? "", r.BookingId, r.CreatedAt);
}

// ── DTOs ──────────────────────────────────────────────────────────────────────

public record ConversationDto(string Id, string TravelerId, string GuideId,
    string? BookingId, DateTime CreatedAt);

public record MessageDto(string Id, string ConversationId, string SenderId,
    string Content, bool IsRead, DateTime CreatedAt);

internal class ConvRow
{
    [JsonPropertyName("id")]          public string? Id { get; set; }
    [JsonPropertyName("traveler_id")] public string? TravelerId { get; set; }
    [JsonPropertyName("guide_id")]    public string? GuideId { get; set; }
    [JsonPropertyName("booking_id")]  public string? BookingId { get; set; }
    [JsonPropertyName("created_at")]  public DateTime CreatedAt { get; set; }
}

internal class MsgRow
{
    [JsonPropertyName("id")]              public string? Id { get; set; }
    [JsonPropertyName("conversation_id")] public string? ConversationId { get; set; }
    [JsonPropertyName("sender_id")]       public string? SenderId { get; set; }
    [JsonPropertyName("content")]         public string? Content { get; set; }
    [JsonPropertyName("is_read")]         public bool IsRead { get; set; }
    [JsonPropertyName("created_at")]      public DateTime CreatedAt { get; set; }
}
