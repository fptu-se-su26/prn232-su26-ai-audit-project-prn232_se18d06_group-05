namespace TripMate_WebAPI.DTOs.Survey;

/// <summary>
/// Survey/Review data transfer object
/// Maps to public.reviews in database_setup.sql
/// </summary>
public record SurveyDto(
    string Id,
    string GuideProfileId,      // was: TourId
    string TravelerId,
    string TravelerName,
    string? BookingId,
    int Rating,
    string Comment,
    bool IsPublished,
    DateTime CreatedAt
);
