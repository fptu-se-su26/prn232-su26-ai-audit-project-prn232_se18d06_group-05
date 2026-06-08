namespace TripMate_WebAPI.DTOs.Tour;

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
