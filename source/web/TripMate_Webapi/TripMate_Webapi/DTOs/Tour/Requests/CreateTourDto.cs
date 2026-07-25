using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace TripMate_WebAPI.DTOs.Tour.Requests;

public class CreateTourDto
{
    public string? Id { get; set; }

    [Required, StringLength(120, MinimumLength = 5)]
    public string Title { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string City { get; set; } = string.Empty;

    [Required, StringLength(240)]
    public string MeetingPoint { get; set; } = string.Empty;

    [Required, StringLength(4000, MinimumLength = 20)]
    public string Description { get; set; } = string.Empty;

    // Legacy compatibility field. Guide schedule writes keep this value in
    // sync until the shared Traveler/API flow adopts the new contract.
    [Range(0.5, 720)]
    public decimal DurationHours { get; set; } = 4;

    public string? DurationType { get; set; }

    [Range(30, 43_200)]
    public int? DurationMinutes { get; set; }

    [Range(1, 30)]
    public int? DurationDays { get; set; }

    public string? DefaultStartTime { get; set; }
    public string? DefaultEndTime { get; set; }

    [StringLength(100)]
    public string? TimeZone { get; set; }

    [Range(1, 1_000_000_000)]
    public decimal PricePerSession { get; set; }

    [Range(0, 1_000_000_000)]
    public decimal PricePerGuest { get; set; }

    [Range(1, 50)]
    public int IncludedGuestCount { get; set; } = 1;

    [Range(1, 50)]
    public int MaxGroupSize { get; set; }

    public string IncludedServices { get; set; } = "[]";
    public string Languages { get; set; } = "[]";
    public string Tags { get; set; } = "[]";

    // TimelineJson remains available while the Guide editor is migrated.
    // ItineraryJson is the typed multi-day contract introduced in Phase 2.
    public string TimelineJson { get; set; } = "[]";
    public string? ItineraryJson { get; set; }
    public string RetainedGalleryImages { get; set; } = "[]";

    public IFormFile? CoverImage { get; set; }
    public List<IFormFile>? GalleryImages { get; set; }
}
