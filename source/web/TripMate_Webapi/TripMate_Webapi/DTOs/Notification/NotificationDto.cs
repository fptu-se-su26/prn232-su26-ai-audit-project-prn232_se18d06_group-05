using System.Text.Json.Serialization;

namespace TripMate_WebAPI.DTOs.Notification;

public sealed class NotificationDto
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("user_id")] public string? UserId { get; set; }
    [JsonPropertyName("type")] public string Type { get; set; } = string.Empty;
    [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;
    [JsonPropertyName("message")] public string Message { get; set; } = string.Empty;
    [JsonPropertyName("link_url")] public string? ActionUrl { get; set; }
    [JsonPropertyName("is_read")] public bool? IsRead { get; set; }
    [JsonPropertyName("created_at")] public DateTime? CreatedAt { get; set; }

    // Compatibility fields used by the existing admin_notifications table.
    [JsonPropertyName("guide_id")] public string? GuideId { get; set; }
    [JsonPropertyName("guide_name")] public string? GuideName { get; set; }
    [JsonPropertyName("guide_email")] public string? GuideEmail { get; set; }
}

public sealed record NotificationPageDto(
    IReadOnlyList<NotificationDto> Items,
    int UnreadCount,
    string? NextCursor);

public sealed record NotificationSendRequest(
    string UserId,
    string Type,
    string Title,
    string Message,
    object? Data = null,
    string? ActionUrl = null,
    string? DedupeKey = null,
    bool SendEmail = false);

public sealed record SystemAnnouncementRequest(
    string Title,
    string Message,
    string? Role = null,
    string? ActionUrl = null,
    bool SendEmail = false);

public sealed record SupportTicketUpdateRequest(
    string UserId,
    string TicketId,
    string Status,
    string Message,
    bool SendEmail = false);
