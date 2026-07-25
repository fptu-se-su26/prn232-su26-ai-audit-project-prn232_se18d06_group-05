using System.Text.Json.Serialization;

namespace TripMate_WebAPI.DTOs.Tour.Scheduling;

/// <summary>
/// A calendar-day itinerary stored in experience_packages.timeline_json.
/// The loose legacy timeline is intentionally not duplicated in a new column.
/// </summary>
public sealed class TourItineraryDayDto
{
    [JsonPropertyName("dayNumber")]
    public int DayNumber { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("items")]
    public List<TourItineraryItemDto> Items { get; set; } = [];
}

public sealed class TourItineraryItemDto
{
    [JsonPropertyName("startTime")]
    public string StartTime { get; set; } = string.Empty;

    [JsonPropertyName("endTime")]
    public string EndTime { get; set; } = string.Empty;

    [JsonPropertyName("activity")]
    public string Activity { get; set; } = string.Empty;

    [JsonPropertyName("location")]
    public string? Location { get; set; }
}
