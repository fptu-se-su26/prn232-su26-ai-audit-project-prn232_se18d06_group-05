namespace TripMate_WebAPI.DTOs.Survey;

/// <summary>
/// Survey with tour information for traveler's history
/// </summary>
public record TravelerSurveyDto(
    string Id,
    string TourId,
    string TourTitle,
    string TourLocation,
    int Rating,
    string Comment,
    DateTime CreatedAt
);
