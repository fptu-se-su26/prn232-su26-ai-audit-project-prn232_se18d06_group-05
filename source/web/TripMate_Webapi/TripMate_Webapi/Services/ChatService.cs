using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TripMate_WebAPI.Services;

/// <summary>
/// ChatService — refactored to use chat_messages table from database_setup.sql.
/// No separate conversations/messages tables. A permanent conversation is
/// simulated by grouping chat_messages by the two participant user IDs.
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
    [JsonPropertyName("role")] public string? Role { get; set; }
}

public record ProfileDto(string Id, string FullName, string? AvatarUrl, string? Role = null);

    // ── Resolve a participant conversation to a backing booking ───────────────

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

    // ── Get my conversations (= one conversation per other user) ─────────────

    public async Task<List<ConversationDto>> GetMyConversationsAsync(
        string userId, string userToken, string? userRole = null)
    {
        // Get all messages where I'm sender or receiver, ordered by most recent
        var url = $"{_supabaseUrl}/rest/v1/chat_messages" +
                  $"?or=(sender_id.eq.{userId},receiver_id.eq.{userId})" +
                  $"&order=sent_at.desc" +
                  $"&select=booking_id,sender_id,receiver_id,sent_at";

        var messages = await GetAsync<List<ChatMessageRow>>(url, userToken) ?? [];

        // A user pair owns one permanent thread even when they have several
        // bookings together. Keep the booking from the newest message only as
        // the backing booking used for subsequent sends.
        var conversations = messages
            .Where(m => !string.IsNullOrWhiteSpace(m.SenderId)
                     && !string.IsNullOrWhiteSpace(m.ReceiverId))
            .GroupBy(
                m => string.Equals(m.SenderId, userId, StringComparison.Ordinal)
                    ? m.ReceiverId!
                    : m.SenderId!,
                StringComparer.Ordinal)
            .Select(g =>
            {
                var first = g.First();
                var otherId = g.Key;
                var isGuide = string.Equals(userRole, "guide", StringComparison.OrdinalIgnoreCase);
                var travelerId = isGuide ? otherId : userId;
                var guideId = isGuide ? userId : otherId;

                return new ConversationDto(
                    BookingId: first.BookingId ?? "",
                    TravelerId: travelerId,
                    GuideId: guideId,
                    CreatedAt: g.Min(m => m.SentAt)
                );
            })
            .ToList();

        return conversations;
    }

    public async Task<int> GetUnreadConversationCountAsync(string userId, string userToken)
    {
        if (string.IsNullOrWhiteSpace(userId)) return 0;

        var url = $"{_supabaseUrl}/rest/v1/chat_messages" +
                  $"?receiver_id=eq.{Uri.EscapeDataString(userId)}" +
                  "&is_read=eq.false&select=sender_id";
        var unreadMessages = await GetAsync<List<ChatMessageRow>>(url, userToken) ?? [];

        return unreadMessages
            .Select(message => message.SenderId)
            .Where(senderId => !string.IsNullOrWhiteSpace(senderId))
            .Distinct(StringComparer.Ordinal)
            .Count();
    }

    // ── Get messages by booking_id ────────────────────────────────────────────

    public async Task<List<MessageDto>> GetMessagesAsync(
        string bookingId, string userToken, int limit = 50, int offset = 0)
    {
        // Return newest-first page using desc ordering. Client will reverse to chronological display.
        var url = $"{_supabaseUrl}/rest/v1/chat_messages" +
                  $"?booking_id=eq.{bookingId}" +
                  $"&order=sent_at.desc" +
                  $"&limit={limit}" +
                  $"&offset={offset}" +
                  $"&select=*";

        var rows = await GetAsync<List<ChatMessageRow>>(url, userToken) ?? new List<ChatMessageRow>();
        return rows.Select(r => new MessageDto(
            r.Id, r.BookingId ?? "", r.SenderId ?? "", r.ReceiverId ?? "",
            r.MessageText ?? "", r.IsRead, r.SentAt)).ToList();
    }

    /// <summary>
    /// Returns the single combined message history between two users, regardless
    /// of which booking originally carried each message.
    /// </summary>
    public async Task<List<MessageDto>> GetMessagesWithUserAsync(
        string userId, string otherUserId, string userToken, int limit = 50, int offset = 0)
    {
        var url = $"{_supabaseUrl}/rest/v1/chat_messages" +
                  $"?or=(and(sender_id.eq.{userId},receiver_id.eq.{otherUserId})," +
                  $"and(sender_id.eq.{otherUserId},receiver_id.eq.{userId}))" +
                  "&order=sent_at.desc" +
                  $"&limit={limit}" +
                  $"&offset={offset}" +
                  "&select=*";

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
        await EnsureChatBookingIsConfirmedAsync(
            bookingId, senderId, receiverId, userToken);

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
        var result = new MessageDto(
            row.Id, row.BookingId ?? "", row.SenderId ?? "", row.ReceiverId ?? "",
            row.MessageText ?? "", row.IsRead, row.SentAt);

        return result;
    }

    /// <summary>
    /// Verifies that the backing booking is confirmed and belongs to the two
    /// chat participants. This is the server-side enforcement for chat access.
    /// </summary>
    public async Task EnsureChatBookingIsConfirmedAsync(
        string bookingId, string firstUserId, string secondUserId, string userToken)
    {
        var escapedBookingId = Uri.EscapeDataString(bookingId);
        var bookingUrl = $"{_supabaseUrl}/rest/v1/bookings" +
                         $"?id=eq.{escapedBookingId}" +
                         "&select=id,traveler_id,guide_profile_id,status&limit=1";
        var bookings = await GetAsync<List<ChatBookingRow>>(bookingUrl, userToken) ?? [];
        var booking = bookings.FirstOrDefault()
            ?? throw new InvalidOperationException("The booking for this chat was not found.");

        if (booking.Status != 1)
        {
            throw new InvalidOperationException(
                "Chat is available only while at least one booking with this person is confirmed.");
        }

        var escapedGuideProfileId = Uri.EscapeDataString(booking.GuideProfileId ?? "");
        var guideUrl = $"{_supabaseUrl}/rest/v1/guide_profiles" +
                       $"?id=eq.{escapedGuideProfileId}&select=user_id&limit=1";
        var guides = await GetAsync<List<ChatGuideProfileRow>>(guideUrl, userToken) ?? [];
        var guideUserId = guides.FirstOrDefault()?.UserId;

        var participantsMatch =
            (SameUserId(booking.TravelerId, firstUserId) && SameUserId(guideUserId, secondUserId)) ||
            (SameUserId(booking.TravelerId, secondUserId) && SameUserId(guideUserId, firstUserId));

        if (!participantsMatch)
        {
            throw new UnauthorizedAccessException(
                "The confirmed booking does not belong to these chat participants.");
        }
    }

    /// <summary>
    /// Edit a message. Only the sender may edit their message.
    /// </summary>
    public async Task<MessageDto> EditMessageAsync(
        string chatBookingId, long messageId, string userId, string content, string userToken)
    {
        // Fetch the existing message row to verify ownership
        var getUrl = $"{_supabaseUrl}/rest/v1/chat_messages?id=eq.{messageId}&select=*";
        var existing = await GetAsync<List<ChatMessageRow>>(getUrl, userToken) ?? new List<ChatMessageRow>();
        var row = existing.FirstOrDefault();
        if (row == null) throw new Exception("Message not found");
        if (!SameUserId(row.SenderId, userId)) throw new UnauthorizedAccessException("Not the message owner");

        var otherUserId = SameUserId(row.SenderId, userId) ? row.ReceiverId : row.SenderId;
        await EnsureChatBookingIsConfirmedAsync(
            chatBookingId, userId ?? "", otherUserId ?? "", userToken);

        var body = new
        {
            message_text = content,
            edited_at = DateTime.UtcNow
        };

        var req = BuildRequest(HttpMethod.Patch, $"{_supabaseUrl}/rest/v1/chat_messages?id=eq.{messageId}", userToken);
        req.Headers.Add("Prefer", "return=representation");
        req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var res = await _http.SendAsync(req);
        var c = await res.Content.ReadAsStringAsync();
        EnsureSuccess(res, c);

        var rows = JsonSerializer.Deserialize<List<ChatMessageRow>>(c, _json);
        var updated = rows!.First();
        return new MessageDto(
            updated.Id, updated.BookingId ?? "", updated.SenderId ?? "", updated.ReceiverId ?? "",
            updated.MessageText ?? "", updated.IsRead, updated.SentAt);
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

    /// <summary>Marks every incoming message from the other participant as read.</summary>
    public async Task MarkMessagesWithUserAsReadAsync(
        string userId, string otherUserId, string userToken)
    {
        var url = $"{_supabaseUrl}/rest/v1/chat_messages" +
                  $"?sender_id=eq.{otherUserId}&receiver_id=eq.{userId}&is_read=eq.false";
        var body = new { is_read = true };

        var req = BuildRequest(HttpMethod.Patch, url, userToken);
        req.Content = new StringContent(
            JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var res = await _http.SendAsync(req);
        var content = await res.Content.ReadAsStringAsync();
        EnsureSuccess(res, content);
    }

    /// <summary>
    /// Get profile (id, full_name, avatar_url) from profiles table
    /// </summary>
    public async Task<ProfileDto?> GetProfileAsync(string userId, string userToken)
    {
        var url = $"{_supabaseUrl}/rest/v1/profiles?id=eq.{userId}&select=id,full_name,avatar_url,role&limit=1";
        var rows = await GetAsync<List<ProfileRow>>(url, userToken);
        var r = rows?.FirstOrDefault();
        if (r == null) return null;
        return new ProfileDto(r.Id ?? "", r.FullName ?? "", r.AvatarUrl, r.Role);
    }

    private static void EnsureSuccess(HttpResponseMessage r, string c)
    {
        if (!r.IsSuccessStatusCode) throw new Exception($"Supabase {r.StatusCode}: {c}");
    }

    private static bool SameUserId(string? left, string? right) =>
        !string.IsNullOrWhiteSpace(left) &&
        string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
}

// ── DTOs ──────────────────────────────────────────────────────────────────────

/// <summary>
/// One participant-pair conversation with a booking used as its send channel.
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

internal class ChatBookingRow
{
    [JsonPropertyName("id")] public string? Id { get; set; }
    [JsonPropertyName("traveler_id")] public string? TravelerId { get; set; }
    [JsonPropertyName("guide_profile_id")] public string? GuideProfileId { get; set; }
    [JsonPropertyName("status")] public int Status { get; set; }
}

internal class ChatGuideProfileRow
{
    [JsonPropertyName("user_id")] public string? UserId { get; set; }
}
