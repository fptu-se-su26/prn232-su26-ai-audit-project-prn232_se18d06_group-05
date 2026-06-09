namespace TripMate_WebAPI.DTOs.Survey;

/// <summary>
/// Error response for survey operations
/// </summary>
public record SurveyErrorResponse(
    bool Success,
    string Error,
    string? Details = null
);
