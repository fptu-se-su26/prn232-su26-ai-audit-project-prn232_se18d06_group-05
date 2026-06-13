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
    public string GuideProfileId { get; set; }

    [Column("title")]
    public string Title { get; set; }

    [Column("description")]
    public string Description { get; set; }

    [Column("duration_hours")]
    public decimal DurationHours { get; set; }

    [Column("price_per_session")]
    public decimal PricePerSession { get; set; }

    [Column("price_per_person")]
    public decimal? PricePerPerson { get; set; }

    [Column("max_group_size")]
    public int MaxGroupSize { get; set; } = 6;

    [Column("included_items")]
    public List<string>? IncludedItems { get; set; }

    [Column("tags")]
    public List<string>? Tags { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; } = true;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Reference(typeof(GuideProfileEntity))]
    public GuideProfileEntity? GuideProfile { get; set; }
}
