namespace TripMate_WebAPI.DTOs.Booking;

/// <summary>
/// Guide availability (blocked dates) DTO
/// Maps to public.guide_availability in database_setup.sql
/// The guide_availability table is a BLACKLIST of unavailable dates.
/// </summary>
public record GuideAvailabilityDto(
    string Id,
    string GuideProfileId,
    string UnavailableDate,     // yyyy-MM-dd format
    string? Reason
);
