namespace TripMate_WebAPI.DTOs.Tour;

/// <summary>
/// Request to create a new experience package
/// Maps to public.experience_packages in database_setup.sql
/// </summary>
public record CreateTourRequest(
    string Title,
    string Description,
    decimal DurationHours,
    decimal PricePerSession,
    decimal? PricePerPerson = null,
    int MaxGroupSize = 6,
    List<string>? IncludedItems = null,
    List<string>? Tags = null
);
