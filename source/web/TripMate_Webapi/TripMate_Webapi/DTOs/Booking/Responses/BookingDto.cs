namespace TripMate_WebAPI.DTOs.Booking;

/// <summary>
/// Booking response DTO
/// Maps to public.bookings + joins in database_setup.sql
/// </summary>
public record BookingDto(
    string Id,
    string TravelerId,
    string GuideProfileId,
    string ExperiencePackageId,
    string? PackageTitle,
    string BookingDate,
    string StartTime,
    int GuestCount,
    decimal TotalAmount,
    decimal PlatformFee,
    decimal GuideEarnings,
    string Status,              // mapped from smallint: pending/confirmed/completed/cancelled
    string? TravelerNotes,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
