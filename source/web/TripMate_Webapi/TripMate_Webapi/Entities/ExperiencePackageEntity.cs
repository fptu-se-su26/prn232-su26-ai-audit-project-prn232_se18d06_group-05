using Postgrest.Attributes;
using Postgrest.Models;
using System;
using System.Collections.Generic;

namespace TripMate_Webapi.Entities;

[Table("experience_packages")]
public class ExperiencePackageEntity : BaseModel
{
    [PrimaryKey("id", false)]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Column("guide_profile_id")]
    public string GuideProfileId { get; set; } = string.Empty;

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    public string Description { get; set; } = string.Empty;

    [Column("duration_hours")]
    public decimal DurationHours { get; set; }

    // Kept alongside DurationHours while the shared Traveler/API flow migrates.
    [Column("duration_type")]
    public string DurationType { get; set; } = "same_day";

    [Column("duration_minutes")]
    public int? DurationMinutes { get; set; }

    [Column("duration_days")]
    public int DurationDays { get; set; } = 1;

    // PostgreSQL time values are represented as HH:mm strings because the
    // current Supabase/PostgREST model layer already exchanges time as text.
    [Column("default_start_time")]
    public string? DefaultStartTime { get; set; }

    [Column("default_end_time")]
    public string? DefaultEndTime { get; set; }

    [Column("time_zone")]
    public string TimeZone { get; set; } = "Asia/Ho_Chi_Minh";

    [Column("price_per_session")]
    public decimal PricePerSession { get; set; }

    [Column("price_per_person")]
    public decimal? PricePerPerson { get; set; } // Additional fee above IncludedGuestCount

    [Column("included_guest_count")]
    public int IncludedGuestCount { get; set; } = 1;

    [Column("max_group_size")]
    public int MaxGroupSize { get; set; } = 6;

    [Column("included_items")]
    public List<string>? IncludedItems { get; set; }

    [Column("tags")]
    public List<string>? Tags { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("publication_status")]
    public string PublicationStatus { get; set; } = "published";

    [Column("city")]
    public string City { get; set; } = string.Empty;

    [Column("meeting_point")]
    public string MeetingPoint { get; set; } = string.Empty;

    [Column("languages")]
    public List<string>? Languages { get; set; }

    [Column("cover_image_url")]
    public string CoverImageUrl { get; set; } = string.Empty;

    [Column("gallery_image_urls")]
    public List<string>? GalleryImageUrls { get; set; }

    [Column("timeline_json")]
    public List<Dictionary<string, string>>? TimelineJson { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Reference(typeof(GuideProfileEntity))]
    public GuideProfileEntity? GuideProfile { get; set; }
}
