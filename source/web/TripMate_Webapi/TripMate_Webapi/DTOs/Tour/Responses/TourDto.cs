namespace TripMate_WebAPI.DTOs.Tour;

public record TourDto(
    string Id,
    string GuideId,
    string Title,
    string? Description,
    string Location,
    double Price,
    int DurationHours,
    int MaxParticipants,
    List<string> Images,
    double Rating,
    int TotalReviews,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
