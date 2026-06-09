namespace TripMate_WebAPI.DTOs.Survey;

/// <summary>
/// Survey with guide info for traveler's history
/// </summary>
public record TravelerSurveyDto(
    string Id,
    string GuideProfileId,      // was: TourId
    string GuideName,           // was: TourTitle - now from guide profile
    string GuideArea,           // was: TourLocation - now city_area from guide_profiles
    int Rating,
    string Comment,
    DateTime CreatedAt
);
