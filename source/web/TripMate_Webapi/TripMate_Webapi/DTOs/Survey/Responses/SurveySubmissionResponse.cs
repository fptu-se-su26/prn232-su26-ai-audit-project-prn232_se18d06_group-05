namespace TripMate_WebAPI.DTOs.Survey;

/// <summary>
/// Response after successfully submitting a survey
/// </summary>
public record SurveySubmissionResponse(
    bool Success,
    string Message,
    SurveyDto? Survey,
    DiscountVoucherDto? DiscountVoucher
);
