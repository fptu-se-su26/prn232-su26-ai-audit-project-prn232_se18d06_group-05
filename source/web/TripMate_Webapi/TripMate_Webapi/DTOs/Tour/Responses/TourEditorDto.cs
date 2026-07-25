using TripMate_WebAPI.DTOs.Tour.Scheduling;

namespace TripMate_WebAPI.DTOs.Tour.Responses;

/// <summary>
/// Guide editor view contract. Keeps the persistence entity out of Razor and
/// exposes both the structured itinerary and a temporary legacy projection.
/// </summary>
public sealed class TourEditorDto
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public decimal DurationHours { get; set; }
    public TourScheduleDto Schedule { get; set; } = new();
    public int MaxGroupSize { get; set; }
    public string City { get; set; } = string.Empty;
    public string MeetingPoint { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal PricePerSession { get; set; }
    public decimal AdditionalGuestFee { get; set; }
    public int IncludedGuestCount { get; set; }
    public List<TourItineraryDayDto> ItineraryDays { get; set; } = [];
    public List<Dictionary<string, string>> TimelineJson { get; set; } = [];
    public List<string> Languages { get; set; } = [];
    public List<string> IncludedItems { get; set; } = [];
    public List<string> Tags { get; set; } = [];
    public string CoverImageUrl { get; set; } = string.Empty;
    public List<string> GalleryImageUrls { get; set; } = [];
    public string PublicationStatus { get; set; } = "draft";
}
