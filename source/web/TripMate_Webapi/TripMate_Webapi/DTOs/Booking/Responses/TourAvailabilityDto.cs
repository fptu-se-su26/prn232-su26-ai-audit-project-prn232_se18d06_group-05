namespace TripMate_WebAPI.DTOs.Booking;

public record TourAvailabilityDto(
    string Id,
    string GuideTourId,
    DateOnly Date,
    int RemainingSlots
);
