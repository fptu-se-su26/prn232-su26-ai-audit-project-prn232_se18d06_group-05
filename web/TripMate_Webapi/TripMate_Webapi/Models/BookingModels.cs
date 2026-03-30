namespace TripMate_WebAPI.Models;

public record CreateBookingRequest(
    string TourId,
    DateOnly TourDate,
    int Guests,
    string? Note
);

public record BookingDto(
    string Id,
    string TourId,
    string TourTitle,
    string TourLocation,
    string TravelerId,
    DateOnly TourDate,
    int Guests,
    double UnitPrice,
    double TotalPrice,
    string? Note,
    string Status,
    DateTime CreatedAt
);
