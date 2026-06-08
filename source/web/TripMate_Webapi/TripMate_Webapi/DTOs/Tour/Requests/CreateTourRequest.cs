namespace TripMate_WebAPI.DTOs.Tour;

public record CreateTourRequest(
    string Title,
    string? Description,
    string Location,
    double Price,
    int DurationHours,
    int MaxParticipants = 10,
    List<string>? Images = null
);
