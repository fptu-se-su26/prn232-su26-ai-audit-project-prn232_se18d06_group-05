namespace TripMate_WebAPI.Models;

// ── Response ──────────────────────────────────────────────────────────────────

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

public record ToursResponse(List<TourDto> Tours, int Total);

// ── Request ───────────────────────────────────────────────────────────────────

public record CreateTourRequest(
    string Title,
    string? Description,
    string Location,
    double Price,
    int DurationHours,
    int MaxParticipants = 10,
    List<string>? Images = null
);

public record UpdateTourRequest(
    string? Title,
    string? Description,
    string? Location,
    double? Price,
    int? DurationHours,
    int? MaxParticipants,
    List<string>? Images,
    string? Status
);
