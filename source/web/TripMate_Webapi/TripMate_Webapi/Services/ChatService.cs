using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TripMate_WebAPI.Services;

/// <summary>
/// ChatService — refactored to use chat_messages table from database_setup.sql.
/// No separate conversations/messages tables. Conversations are simulated
/// by grouping chat_messages by booking_id.
/// </summary>
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

internal class ProfileRow
{
    [JsonPropertyName("id")] public string? Id { get; set; }
    [JsonPropertyName("full_name")] public string? FullName { get; set; }
    [JsonPropertyName("avatar_url")] public string? AvatarUrl { get; set; }
}

public record ProfileDto(string Id, string FullName, string? AvatarUrl);

    // ── Get or create "conversation" (= messages grouped by booking_id) ───────

    public async Task<ConversationDto> GetOrCreateConversationAsync(
        string travelerId, string guideId, string bookingId, string userToken)
    {
        // In the new schema, a "conversation" is just a booking_id grouping.
        // We return metadata about the conversation.
        // Check if any messages exist for this booking
        var url = $"{_supabaseUrl}/rest/v1/chat_messages" +
                  $"?booking_id=eq.{bookingId}" +
                  $"&limit=1&select=id,sent_at";

        var existing = await GetAsync<List<ChatMessageRow>>(url, userToken);

        var createdAt = existing?.FirstOrDefault()?.SentAt ?? DateTime.UtcNow;

        return new ConversationDto(
            BookingId: bookingId,
            TravelerId: travelerId,
            GuideId: guideId,
            CreatedAt: createdAt
        );
    }

    // ── Get my conversations (= distinct booking_ids I have messages in) ─────

    public async Task<List<ConversationDto>> GetMyConversationsAsync(
        string userId, string userToken)
    {
        // Get all messages where I'm sender or receiver, ordered by most recent
        var url = $"{_supabaseUrl}/rest/v1/chat_messages" +
                  $"?or=(sender_id.eq.{userId},receiver_id.eq.{userId})" +
                  $"&order=sent_at.desc" +
                  $"&select=booking_id,sender_id,receiver_id,sent_at";

        var messages = await GetAsync<List<ChatMessageRow>>(url, userToken) ?? [];

        // Group by booking_id → simulate conversations
        var conversations = messages
            .GroupBy(m => m.BookingId)
            .Select(g =>
            {
                var first = g.First();
                var senderId = first.SenderId ?? "";
                var receiverId = first.ReceiverId ?? "";

                // Determine traveler vs guide
                var travelerId = senderId == userId ? senderId : receiverId;
                var guideId = senderId == userId ? receiverId : senderId;

                return new ConversationDto(
                    BookingId: g.Key ?? "",
                    TravelerId: travelerId,
                    GuideId: guideId,
                    CreatedAt: g.Min(m => m.SentAt)
                );
            })
            .ToList();

        return conversations;
    }

    // ── Get messages by booking_id ────────────────────────────────────────────

    public async Task<List<MessageDto>> GetMessagesAsync(
        string bookingId, string userToken)
    {
        var url = $"{_supabaseUrl}/rest/v1/chat_messages" +
                  $"?booking_id=eq.{bookingId}" +
                  $"&order=sent_at.asc" +
                  $"&select=*";

        var rows = await GetAsync<List<ChatMessageRow>>(url, userToken) ?? [];
        return rows.Select(r => new MessageDto(
            r.Id, r.BookingId ?? "", r.SenderId ?? "", r.ReceiverId ?? "",
            r.MessageText ?? "", r.IsRead, r.SentAt)).ToList();
    }

    // ── Send message ──────────────────────────────────────────────────────────

    public async Task<MessageDto> SendMessageAsync(
        string bookingId, string senderId, string receiverId,
        string content, string userToken)
    {
        var body = new
        {
            booking_id = bookingId,
            sender_id = senderId,
            receiver_id = receiverId,
            message_text = content,
            is_read = false,
        };

        var req = BuildRequest(HttpMethod.Post,
            $"{_supabaseUrl}/rest/v1/chat_messages", userToken);
        req.Headers.Add("Prefer", "return=representation");
        req.Content = new StringContent(
            JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var res = await _http.SendAsync(req);
        var c = await res.Content.ReadAsStringAsync();
        EnsureSuccess(res, c);

        var rows = JsonSerializer.Deserialize<List<ChatMessageRow>>(c, _json);
        var row = rows!.First();
        return new MessageDto(
            row.Id, row.BookingId ?? "", row.SenderId ?? "", row.ReceiverId ?? "",
            row.MessageText ?? "", row.IsRead, row.SentAt);
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

    /// <summary>
    /// Mark all messages for a booking as read where receiver_id == userId
    /// </summary>
    public async Task MarkMessagesAsReadAsync(string bookingId, string userId, string userToken)
    {
        var url = $"{_supabaseUrl}/rest/v1/chat_messages?booking_id=eq.{bookingId}&receiver_id=eq.{userId}";
        var body = new { is_read = true };

        var req = BuildRequest(HttpMethod.Patch, url, userToken);
        req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var res = await _http.SendAsync(req);
        var c = await res.Content.ReadAsStringAsync();
        if (!res.IsSuccessStatusCode)
        {
            // don't throw to avoid breaking UI; log via exception
            throw new Exception($"Supabase mark-read failed: {res.StatusCode}: {c}");
        }
    }

    /// <summary>
    /// Get profile (id, full_name, avatar_url) from profiles table
    /// </summary>
    public async Task<ProfileDto?> GetProfileAsync(string userId, string userToken)
    {
        var url = $"{_supabaseUrl}/rest/v1/profiles?id=eq.{userId}&select=id,full_name,avatar_url&limit=1";
        var rows = await GetAsync<List<ProfileRow>>(url, userToken);
        var r = rows?.FirstOrDefault();
        if (r == null) return null;
        return new ProfileDto(r.Id ?? "", r.FullName ?? "", r.AvatarUrl);
    }

    private static void EnsureSuccess(HttpResponseMessage r, string c)
    {
        if (!r.IsSuccessStatusCode) throw new Exception($"Supabase {r.StatusCode}: {c}");
    }
}

// ── DTOs ──────────────────────────────────────────────────────────────────────

/// <summary>
/// Simulated conversation = messages grouped by booking_id
/// </summary>
public record ConversationDto(
    string BookingId,
    string TravelerId,
    string GuideId,
    DateTime CreatedAt);

/// <summary>
/// Maps to a row in public.chat_messages
/// </summary>
public record MessageDto(
    long Id,
    string BookingId,
    string SenderId,
    string ReceiverId,
    string MessageText,
    bool IsRead,
    DateTime SentAt);

// ── Request DTOs ──────────────────────────────────────────────────────────────

public record CreateConversationRequest(string GuideId, string BookingId);
public record SendMessageRequest(string Content, string ReceiverId);

// ── Internal row model ────────────────────────────────────────────────────────

internal class ChatMessageRow
{
    [JsonPropertyName("id")]           public long Id { get; set; }
    [JsonPropertyName("booking_id")]   public string? BookingId { get; set; }
    [JsonPropertyName("sender_id")]    public string? SenderId { get; set; }
    [JsonPropertyName("receiver_id")]  public string? ReceiverId { get; set; }
    [JsonPropertyName("message_text")] public string? MessageText { get; set; }
    [JsonPropertyName("is_read")]      public bool IsRead { get; set; }
    [JsonPropertyName("sent_at")]      public DateTime SentAt { get; set; }
    [JsonPropertyName("edited_at")]    public DateTime? EditedAt { get; set; }
}
