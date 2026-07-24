using System.Text.Json.Serialization;

namespace TripMate_Webapi.Repositories.Models;

public sealed class CalendarBookingRecord
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("traveler_id")]
    public string TravelerId { get; set; } = string.Empty;

    [JsonPropertyName("experience_package_id")]
    public string ExperiencePackageId { get; set; } = string.Empty;

    [JsonPropertyName("booking_date")]
    public string BookingDate { get; set; } = string.Empty;

    [JsonPropertyName("start_time")]
    public string StartTime { get; set; } = string.Empty;

    [JsonPropertyName("guest_count")]
    public int GuestCount { get; set; }

    [JsonPropertyName("guide_earnings")]
    public decimal GuideEarnings { get; set; }

    [JsonPropertyName("status")]
    public int Status { get; set; }

    [JsonPropertyName("traveler_notes")]
    public string? TravelerNotes { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("traveler")]
    public CalendarTravelerRecord? Traveler { get; set; }

    [JsonPropertyName("experience_package")]
    public CalendarPackageRecord? ExperiencePackage { get; set; }
}

public sealed class CalendarTravelerRecord
{
    [JsonPropertyName("full_name")]
    public string? FullName { get; set; }

    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; set; }
}

public sealed class CalendarPackageRecord
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("duration_hours")]
    public decimal DurationHours { get; set; }

    [JsonPropertyName("meeting_point")]
    public string? MeetingPoint { get; set; }

    [JsonPropertyName("cover_image_url")]
    public string? CoverImageUrl { get; set; }
}
