namespace TripMate_WebAPI.DTOs.Booking.Responses;

public record GuideBookingViewDto(
    string Id,

    // Traveler id
    string TravelerId,

    // Traveler info (from profiles)
    string TravelerName,
    string TravelerAvatar,
    decimal TravelerRating,
    string TravelerLocation,

    // Tour info (from experience_packages)
    string TourName,

    // Booking details
    string Date,
    string Time,
    int Guests,
    decimal TotalAmount,
    decimal PlatformFee,
    decimal NetEarnings,
    string? Note,
    string Status,

    // UI logic
    int SecondsRemaining
);
