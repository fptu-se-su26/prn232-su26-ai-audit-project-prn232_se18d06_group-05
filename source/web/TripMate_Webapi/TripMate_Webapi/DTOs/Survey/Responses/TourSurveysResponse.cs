namespace TripMate_WebAPI.DTOs.Survey;

/// <summary>
/// Response containing list of surveys for a tour
/// </summary>
public record TourSurveysResponse(
    List<SurveyDto> Surveys,
    int Total,
    double AverageRating
);
