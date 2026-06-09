namespace TripMate_WebAPI.DTOs.Survey;

/// <summary>
/// Request model for submitting a review
/// Maps to public.reviews in database_setup.sql
/// </summary>
public record SubmitSurveyRequest(
    string GuideProfileId,      // UUID → guide_profiles.id (was: TourId)
    string BookingId,            // UUID → bookings.id
    int Rating,                  // 1-5 CHECK constraint in DB
    string Comment
);
