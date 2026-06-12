namespace TripMate_WebAPI.DTOs.Survey;

/// <summary>
/// Guide rating summary (was: TourRatingDto)
/// </summary>
public record TourRatingDto(
    string GuideProfileId,      // was: TourId
    string GuideName,           // was: TourTitle
    double Rating,
    int TotalReviews
);
