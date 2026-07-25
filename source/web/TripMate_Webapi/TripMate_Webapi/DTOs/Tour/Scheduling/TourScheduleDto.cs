using System.Text.Json.Serialization;

namespace TripMate_WebAPI.DTOs.Tour.Scheduling;

/// <summary>
/// Shared Guide-side schedule contract for both published tours and drafts.
/// Clock values use the local HH:mm format and are interpreted in TimeZone.
/// </summary>
public sealed class TourScheduleDto
{
    [JsonPropertyName("durationType")]
    public string DurationType { get; set; } = TourDurationTypes.SameDay;

    [JsonPropertyName("durationMinutes")]
    public int? DurationMinutes { get; set; }

    [JsonPropertyName("durationDays")]
    public int DurationDays { get; set; } = 1;

    [JsonPropertyName("defaultStartTime")]
    public string? DefaultStartTime { get; set; }

    [JsonPropertyName("defaultEndTime")]
    public string? DefaultEndTime { get; set; }

    [JsonPropertyName("timeZone")]
    public string TimeZone { get; set; } = "Asia/Ho_Chi_Minh";
}

public static class TourDurationTypes
{
    public const string SameDay = "same_day";
    public const string MultiDay = "multi_day";
}
