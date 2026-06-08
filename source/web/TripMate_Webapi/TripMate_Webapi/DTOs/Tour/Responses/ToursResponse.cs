namespace TripMate_WebAPI.DTOs.Tour;

public record ToursResponse(List<TourDto> Tours, int Total);
