namespace TripMate_WebAPI.Models;

// ── Request Models ────────────────────────────────────────────────────────────

/// <summary>
/// Request model for submitting a tour survey/review
/// </summary>
public record SubmitSurveyRequest(
    string TourId,
    string BookingId,
    int Rating,
    string Comment
);

// ── Response Models ───────────────────────────────────────────────────────────

/// <summary>
/// Response after successfully submitting a survey
/// </summary>
public record SurveySubmissionResponse(
    bool Success,
    string Message,
    SurveyDto? Survey,
    DiscountVoucherDto? DiscountVoucher
);

/// <summary>
/// Survey/Review data transfer object
/// </summary>
public record SurveyDto(
    string Id,
    string TourId,
    string TravelerId,
    string TravelerName,
    string? BookingId,
    int Rating,
    string Comment,
    bool IsPublished,
    DateTime CreatedAt
);

/// <summary>
/// Discount voucher given to first-time survey submitters
/// </summary>
public record DiscountVoucherDto(
    string Code,
    int DiscountPercent,
    DateTime ExpiresAt
);

/// <summary>
/// Response containing list of surveys for a tour
/// </summary>
public record TourSurveysResponse(
    List<SurveyDto> Surveys,
    int Total,
    double AverageRating
);

/// <summary>
/// Response containing traveler's survey history
/// </summary>
public record TravelerSurveysResponse(
    List<TravelerSurveyDto> Surveys,
    int Total
);

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

/// <summary>
/// Tour rating summary
/// </summary>
public record TourRatingDto(
    string TourId,
    string TourTitle,
    double Rating,
    int TotalReviews
);

// ── Error Response ────────────────────────────────────────────────────────────

/// <summary>
/// Error response for survey operations
/// </summary>
public record SurveyErrorResponse(
    bool Success,
    string Error,
    string? Details = null
);
