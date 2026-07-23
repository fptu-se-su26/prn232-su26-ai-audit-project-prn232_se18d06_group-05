using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using TripMate_Webapi.Repositories;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.SignalR;

namespace TripMate_WebAPI.Services;

public interface INotificationService
{
    Task NotifyAdminNewGuideApplicationAsync(string guideId, string guideName, string guideEmail);
    Task<List<NotificationDto>> GetAdminNotificationsAsync(string adminToken);
    Task MarkNotificationAsReadAsync(string notificationId, string adminToken);

    Task SendAsync(
        string userId,
        string type,
        string title,
        string message,
        object? data = null,
        string? actionUrl = null,
        string? dedupeKey = null,
        bool sendEmail = false);

    Task SendToRoleAsync(
        string? role,
        string type,
        string title,
        string message,
        object? data = null,
        string? actionUrl = null,
        string? dedupeKey = null,
        bool sendEmail = false);

    Task<NotificationPageDto> GetForUserAsync(
        string userId,
        string userToken,
        int limit = 30,
        string? before = null,
        bool unreadOnly = false);

    Task<int> GetUnreadCountAsync(string userId, string userToken);
    Task<bool> MarkAsReadAsync(string notificationId, string userId, string userToken);
    Task<bool> MarkAllAsReadAsync(string userId, string userToken);
    Task<bool> DeleteAsync(string notificationId, string userId, string userToken);
}

public sealed class NotificationService : INotificationService
{
    private readonly HttpClient _http;
    private readonly IEmailService _emailService;
    private readonly IHubContext<NotificationHub> _hub;
    private readonly string _supabaseUrl;
    private readonly string _anonKey;
    private readonly string _serviceRoleKey;
    private readonly ILogger<NotificationService> _logger;
    private readonly INotificationRepository _notificationRepository;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public NotificationService(
        HttpClient http,
        IEmailService emailService,
        IHubContext<NotificationHub> hub,
        IConfiguration config,
        ILogger<NotificationService> logger,
        INotificationRepository notificationRepository)
    {
        _http = http;
        _emailService = emailService;
        _hub = hub;
        _supabaseUrl = config["Supabase:Url"] ?? throw new InvalidOperationException("Supabase URL not configured");
        _anonKey = config["Supabase:AnonKey"] ?? throw new InvalidOperationException("Supabase Anon Key not configured");
        _serviceRoleKey = config["Supabase:ServiceRoleKey"] ?? throw new InvalidOperationException("Supabase Service Role Key not configured");
        _logger = logger;
        _notificationRepository = notificationRepository;
    }

    public async Task SendAsync(
        string userId,
        string type,
        string title,
        string message,
        object? data = null,
        string? actionUrl = null,
        string? dedupeKey = null,
        bool sendEmail = false)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("Skipped {Type} notification because recipient user id was empty", type);
            return;
        }

        try
        {
            var notificationId = CreateNotificationId(userId, dedupeKey);
            var payload = new
            {
                id = notificationId,
                user_id = userId,
                type,
                title,
                message,
                link_url = actionUrl,
                is_read = false,
                created_at = DateTime.UtcNow
            };

            // The existing notifications table has no dedupe_key column. A stable
            // primary key preserves idempotency for events that provide a dedupe key.
            var url = $"{_supabaseUrl}/rest/v1/notifications?on_conflict=id";
            using var request = BuildServiceRequest(HttpMethod.Post, url);
            request.Headers.Add("Prefer", "resolution=ignore-duplicates,return=representation");
            request.Content = JsonContent(payload);

            using var response = await _http.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to persist {Type} notification for {UserId}: {Content}", type, userId, content);
                return;
            }

            var rows = JsonSerializer.Deserialize<List<NotificationDto>>(content, JsonOptions) ?? [];
            var created = rows.FirstOrDefault();
            if (created is null)
            {
                _logger.LogDebug("Deduplicated {Type} notification for {UserId} with key {DedupeKey}", type, userId, dedupeKey);
                return;
            }

            await _hub.Clients.User(userId).SendAsync("NotificationReceived", created);

            if (sendEmail)
            {
                var recipient = await GetProfileAsync(userId);
                if (!string.IsNullOrWhiteSpace(recipient?.Email))
                {
                    await _emailService.SendNotificationEmailAsync(
                        recipient.Email,
                        recipient.FullName ?? "TripMate user",
                        title,
                        message,
                        actionUrl);
                }
            }
        }
        catch (Exception ex)
        {
            // Notifications must not roll back the business operation that emitted them.
            _logger.LogError(ex, "Error delivering {Type} notification to {UserId}", type, userId);
        }
    }

    private static Guid CreateNotificationId(string userId, string? dedupeKey)
    {
        if (string.IsNullOrWhiteSpace(dedupeKey)) return Guid.NewGuid();

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{userId}:{dedupeKey}"));
        return new Guid(hash.AsSpan(0, 16));
    }

    public async Task SendToRoleAsync(
        string? role,
        string type,
        string title,
        string message,
        object? data = null,
        string? actionUrl = null,
        string? dedupeKey = null,
        bool sendEmail = false)
    {
        try
        {
            var roleFilter = string.IsNullOrWhiteSpace(role)
                ? string.Empty
                : $"&role=eq.{Uri.EscapeDataString(role)}";
            var url = $"{_supabaseUrl}/rest/v1/profiles?select=id{roleFilter}";
            using var request = BuildServiceRequest(HttpMethod.Get, url);
            using var response = await _http.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to resolve recipients for role {Role}: {Content}", role ?? "all", content);
                return;
            }

            var recipients = JsonSerializer.Deserialize<List<ProfileRecipient>>(content, JsonOptions) ?? [];
            foreach (var recipient in recipients.Where(x => !string.IsNullOrWhiteSpace(x.Id)))
            {
                var recipientKey = dedupeKey is null ? null : $"{dedupeKey}:{recipient.Id}";
                await SendAsync(recipient.Id!, type, title, message, data, actionUrl, recipientKey, sendEmail);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending {Type} notification to role {Role}", type, role ?? "all");
        }
    }

    public async Task<NotificationPageDto> GetForUserAsync(
        string userId,
        string userToken,
        int limit = 30,
        string? before = null,
        bool unreadOnly = false)
    {
        limit = Math.Clamp(limit, 1, 100);
        var filters = new StringBuilder($"&user_id=eq.{Uri.EscapeDataString(userId)}");
        if (unreadOnly) filters.Append("&is_read=eq.false");
        if (DateTimeOffset.TryParse(before, out var cursor))
            filters.Append($"&created_at=lt.{Uri.EscapeDataString(cursor.UtcDateTime.ToString("O"))}");

        var url = $"{_supabaseUrl}/rest/v1/notifications?select=*&order=created_at.desc&limit={limit + 1}{filters}";
        using var request = BuildUserRequest(HttpMethod.Get, url, userToken);
        using var response = await _http.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        EnsureSuccess(response, content, "load notifications");

        var rows = JsonSerializer.Deserialize<List<NotificationDto>>(content, JsonOptions) ?? [];
        var hasMore = rows.Count > limit;
        var items = rows.Take(limit).ToList();
        var nextCursor = hasMore ? items.LastOrDefault()?.CreatedAt?.ToUniversalTime().ToString("O") : null;
        var unreadCount = await GetUnreadCountAsync(userId, userToken);
        return new NotificationPageDto(items, unreadCount, nextCursor);
    }

    public async Task<int> GetUnreadCountAsync(string userId, string userToken)
    {
        var url = $"{_supabaseUrl}/rest/v1/notifications?select=id&user_id=eq.{Uri.EscapeDataString(userId)}&is_read=eq.false&limit=1";
        using var request = BuildUserRequest(HttpMethod.Get, url, userToken);
        request.Headers.Add("Prefer", "count=exact");
        using var response = await _http.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        EnsureSuccess(response, content, "count unread notifications");

        var range = response.Content.Headers.ContentRange?.Length;
        if (range.HasValue) return checked((int)range.Value);

        var rawRange = response.Content.Headers.TryGetValues("Content-Range", out var values)
            ? values.FirstOrDefault()
            : null;
        var totalText = rawRange?.Split('/').LastOrDefault();
        return int.TryParse(totalText, out var total) ? total : 0;
    }

    public async Task<bool> MarkAsReadAsync(string notificationId, string userId, string userToken)
    {
        var url = $"{_supabaseUrl}/rest/v1/notifications?id=eq.{Uri.EscapeDataString(notificationId)}&user_id=eq.{Uri.EscapeDataString(userId)}";
        return await PatchUserNotificationsAsync(url, userToken, new { is_read = true });
    }

    public async Task<bool> MarkAllAsReadAsync(string userId, string userToken)
    {
        var url = $"{_supabaseUrl}/rest/v1/notifications?user_id=eq.{Uri.EscapeDataString(userId)}&is_read=eq.false";
        var databaseAcceptedUpdate = await PatchUserNotificationsAsync(
            url,
            userToken,
            new { is_read = true });

        if (!databaseAcceptedUpdate)
        {
            // An empty update is valid only when there was nothing unread. This also
            // prevents the API from reporting success when RLS silently matched no rows.
            try
            {
                return await GetUnreadCountAsync(userId, userToken) == 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not verify mark-all-as-read for {UserId}", userId);
                return false;
            }
        }

        try
        {
            return await GetUnreadCountAsync(userId, userToken) == 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database update succeeded but unread verification failed for {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> DeleteAsync(string notificationId, string userId, string userToken)
    {
        var url = $"{_supabaseUrl}/rest/v1/notifications?id=eq.{Uri.EscapeDataString(notificationId)}&user_id=eq.{Uri.EscapeDataString(userId)}";
        using var request = BuildUserRequest(HttpMethod.Delete, url, userToken);
        using var response = await _http.SendAsync(request);
        if (response.IsSuccessStatusCode) return true;
        _logger.LogError("Failed to delete notification {NotificationId}: {Content}", notificationId, await response.Content.ReadAsStringAsync());
        return false;
    }

    public async Task NotifyAdminNewGuideApplicationAsync(string guideId, string guideName, string guideEmail)
    {
        await CreateLegacyAdminNotificationAsync(guideId, guideName, guideEmail);
        await SendToRoleAsync(
            "admin",
            NotificationTypes.GuideApplicationSubmitted,
            "New guide application",
            $"{guideName} ({guideEmail}) submitted a guide application.",
            new { guideId, guideName, guideEmail },
            "/Admin/GuideApprovals",
            $"guide-application:{guideId}");
        await NotifyAdminsByEmailAsync(guideName, guideEmail);
    }

    public async Task<List<NotificationDto>> GetAdminNotificationsAsync(string adminToken)
    {
        try
        {
            var url = $"{_supabaseUrl}/rest/v1/admin_notifications?select=*&order=created_at.desc&limit=50";
            using var request = BuildUserRequest(HttpMethod.Get, url, adminToken);
            using var response = await _http.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get legacy admin notifications: {Content}", content);
                return [];
            }

            return JsonSerializer.Deserialize<List<NotificationDto>>(content, JsonOptions) ?? [];
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting legacy admin notifications");
            return [];
        }
    }

    public async Task MarkNotificationAsReadAsync(string notificationId, string adminToken)
    {
        try
        {
            var url = $"{_supabaseUrl}/rest/v1/admin_notifications?id=eq.{Uri.EscapeDataString(notificationId)}";
            using var request = BuildUserRequest(HttpMethod.Patch, url, adminToken);
            request.Content = JsonContent(new { is_read = true });
            using var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                _logger.LogError("Failed to mark legacy admin notification read: {Content}", await response.Content.ReadAsStringAsync());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking legacy admin notification read");
        }
    }

    private async Task<bool> PatchUserNotificationsAsync(string url, string userToken, object body)
    {
        using var request = BuildUserRequest(HttpMethod.Patch, url, userToken);
        request.Headers.Add("Prefer", "return=representation");
        request.Content = JsonContent(body);
        using var response = await _http.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Failed to update notifications: {Content}", content);
            return false;
        }

        var updated = JsonSerializer.Deserialize<List<NotificationDto>>(content, JsonOptions) ?? [];
        return updated.Count > 0;
    }

    private async Task CreateLegacyAdminNotificationAsync(string guideId, string guideName, string guideEmail)
    {
        try
        {
            var payload = new
            {
                id = Guid.NewGuid(),
                type = "guide_application",
                title = "New guide application",
                message = $"{guideName} ({guideEmail}) submitted a guide application and needs review.",
                guide_id = guideId,
                guide_name = guideName,
                guide_email = guideEmail,
                is_read = false,
                created_at = DateTime.UtcNow
            };
            using var request = BuildServiceRequest(HttpMethod.Post, $"{_supabaseUrl}/rest/v1/admin_notifications");
            request.Content = JsonContent(payload);
            using var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                _logger.LogError("Failed to create legacy admin notification: {Content}", await response.Content.ReadAsStringAsync());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating legacy admin notification");
        }
    }

        public async Task SendAsync(string userId, string type, string title, string message, object? data = null)
        {
            try
            {
                string linkUrl = "/Traveler/Trips";
                if (data != null)
                {
                    try {
                        var json = JsonSerializer.Serialize(data);
                        var doc = JsonDocument.Parse(json);
                        if (doc.RootElement.TryGetProperty("bookingId", out var bookingIdProp))
                        {
                            linkUrl = "/Traveler/BookingDetails/" + bookingIdProp.GetString();
                        }
                    } catch {}
                }

                var notification = new TripMate_Webapi.Entities.NotificationEntity
                {
                    Id = Guid.NewGuid().ToString(),
                    UserId = userId,
                    Title = title,
                    Message = message,
                    Type = type,
                    IsRead = false,
                    LinkUrl = linkUrl,
                    CreatedAt = DateTime.UtcNow
                };

                await _notificationRepository.CreateNotificationAsync(notification);
                _logger.LogInformation("Realtime notification sent to user {UserId}: {Type} - {Title}", userId, type, title);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save or send realtime notification to user {UserId}", userId);
            }
        }
    private async Task NotifyAdminsByEmailAsync(string guideName, string guideEmail)
    {
        try
        {
            var admins = await GetProfilesAsync("admin");
            foreach (var admin in admins.Where(x => !string.IsNullOrWhiteSpace(x.Email)))
            {
                await _emailService.SendAdminNotificationEmailAsync(
                    admin.Email!, admin.FullName ?? "Admin", guideName, guideEmail);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending guide-application emails to admins");
        }
    }

    private async Task<ProfileRecipient?> GetProfileAsync(string userId)
        => (await GetProfilesAsync(null, userId)).FirstOrDefault();

    private async Task<List<ProfileRecipient>> GetProfilesAsync(string? role, string? userId = null)
    {
        var filters = string.Empty;
        if (!string.IsNullOrWhiteSpace(role)) filters += $"&role=eq.{Uri.EscapeDataString(role)}";
        if (!string.IsNullOrWhiteSpace(userId)) filters += $"&id=eq.{Uri.EscapeDataString(userId)}";
        var url = $"{_supabaseUrl}/rest/v1/profiles?select=id,email,full_name{filters}";
        using var request = BuildServiceRequest(HttpMethod.Get, url);
        using var response = await _http.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        EnsureSuccess(response, content, "load notification recipients");
        return JsonSerializer.Deserialize<List<ProfileRecipient>>(content, JsonOptions) ?? [];
    }

    private HttpRequestMessage BuildServiceRequest(HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("apikey", _serviceRoleKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _serviceRoleKey);
        return request;
    }

    private HttpRequestMessage BuildUserRequest(HttpMethod method, string url, string userToken)
    {
        var token = userToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? userToken["Bearer ".Length..].Trim()
            : userToken.Trim();
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("apikey", _anonKey);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private static StringContent JsonContent(object value)
        => new(JsonSerializer.Serialize(value, JsonOptions), Encoding.UTF8, "application/json");

    private static void EnsureSuccess(HttpResponseMessage response, string content, string operation)
    {
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Failed to {operation} ({(int)response.StatusCode}): {content}");
    }

    private sealed class ProfileRecipient
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("email")] public string? Email { get; set; }
        [JsonPropertyName("full_name")] public string? FullName { get; set; }
    }
}
