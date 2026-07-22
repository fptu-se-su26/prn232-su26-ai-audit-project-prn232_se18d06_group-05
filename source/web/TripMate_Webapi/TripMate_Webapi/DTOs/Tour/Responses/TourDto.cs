namespace TripMate_WebAPI.DTOs.Tour;

/// <summary>
/// DTO for experience package response
/// Maps to public.experience_packages + join guide_profiles in database_setup.sql
/// </summary>
public record TourDto(
    string Id,
    string GuideProfileId,
    string? GuideName,
    string Title,
    string Description,
    decimal DurationHours,
    decimal PricePerSession,
    decimal? PricePerPerson,
    int IncludedGuestCount,
    int MaxGroupSize,
    List<string> IncludedItems,
    List<string> Tags,
    bool IsActive,
    DateTime CreatedAt
);
