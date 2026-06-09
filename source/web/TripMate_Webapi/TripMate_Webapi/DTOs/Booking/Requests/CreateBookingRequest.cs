namespace TripMate_WebAPI.DTOs.Booking;

/// <summary>
/// Request to create a new booking
/// Maps to public.bookings in database_setup.sql
/// </summary>
public record CreateBookingRequest(
    string ExperiencePackageId,   // UUID → experience_packages.id
    string BookingDate,           // yyyy-MM-dd format
    string StartTime,             // HH:mm format
    int GuestCount = 1,
    string? TravelerNotes = null
);
