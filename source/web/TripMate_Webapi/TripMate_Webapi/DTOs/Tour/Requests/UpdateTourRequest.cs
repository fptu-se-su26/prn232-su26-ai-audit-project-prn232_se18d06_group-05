namespace TripMate_WebAPI.DTOs.Tour;

/// <summary>
/// Request to update an experience package (all fields optional)
/// Maps to public.experience_packages in database_setup.sql
/// </summary>
public record UpdateTourRequest(
    string? Title = null,
    string? Description = null,
    decimal? DurationHours = null,
    decimal? PricePerSession = null,
    decimal? PricePerPerson = null,
    int? MaxGroupSize = null,
    List<string>? IncludedItems = null,
    List<string>? Tags = null,
    bool? IsActive = null
);
