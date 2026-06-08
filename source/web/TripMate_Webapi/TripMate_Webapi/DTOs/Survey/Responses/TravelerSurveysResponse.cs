namespace TripMate_WebAPI.DTOs.Survey;

/// <summary>
/// Response containing traveler's survey history
/// </summary>
public record TravelerSurveysResponse(
    List<TravelerSurveyDto> Surveys,
    int Total
);
