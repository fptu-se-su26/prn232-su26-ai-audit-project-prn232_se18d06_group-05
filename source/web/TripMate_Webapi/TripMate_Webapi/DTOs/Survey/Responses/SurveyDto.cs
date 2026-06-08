namespace TripMate_WebAPI.DTOs.Survey;

/// <summary>
/// Survey/Review data transfer object
/// </summary>
public record SurveyDto(
    string Id,
    string TourId,
    string TravelerId,
    string TravelerName,
    string? BookingId,
    int Rating,
    string Comment,
    bool IsPublished,
    DateTime CreatedAt
);
