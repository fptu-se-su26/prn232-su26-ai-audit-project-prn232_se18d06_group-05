namespace TripMate_WebAPI.DTOs.Survey;

/// <summary>
/// Request model for submitting a tour survey/review
/// </summary>
public record SubmitSurveyRequest(
    string TourId,
    string BookingId,
    int Rating,
    string Comment
);
