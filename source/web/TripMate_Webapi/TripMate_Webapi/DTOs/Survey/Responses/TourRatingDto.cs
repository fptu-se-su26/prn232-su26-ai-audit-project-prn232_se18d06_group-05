namespace TripMate_WebAPI.DTOs.Survey;

/// <summary>
/// Tour rating summary
/// </summary>
public record TourRatingDto(
    string TourId,
    string TourTitle,
    double Rating,
    int TotalReviews
);
