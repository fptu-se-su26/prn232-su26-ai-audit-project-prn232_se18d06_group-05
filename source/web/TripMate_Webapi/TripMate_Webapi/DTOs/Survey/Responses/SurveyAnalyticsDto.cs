namespace TripMate_WebAPI.DTOs.Survey;

/// <summary>
/// Survey analytics for admin dashboard
/// </summary>
public record SurveyAnalyticsDto(
    int TotalSurveys,
    double AverageRating,
    int TotalCompletedBookings,
    double SubmissionRate,
    TourRatingDto? HighestRatedTour,
    TourRatingDto? LowestRatedTour,
    Dictionary<int, int> RatingDistribution
);
