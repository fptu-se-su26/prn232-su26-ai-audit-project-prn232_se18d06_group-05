using Postgrest.Attributes;
using Postgrest.Models;

namespace TripMate_Webapi.Entities
{
    [Table("guide_profiles")]
    public class GuideProfileEntity : BaseModel
    {
        [PrimaryKey("id", true)]
        public string Id { get; set; }

        [Column("user_id")]
        public string UserId { get; set; }

        [Reference(typeof(ProfileEntity))]
        public ProfileEntity Profile { get; set; }

        [Column("bio")]
        public string Bio { get; set; }

        [Column("languages")]
        public List<string>? Languages { get; set; }

        [Column("specialties")]
        public List<string>? Specialties { get; set; }

        [Column("city_area")]
        public string? CityArea { get; set; }

        [Column("price_per_hour")]
        public decimal? PricePerHour { get; set; }

        [Column("is_verified")]
        public bool? IsVerified { get; set; }

        [Column("verified_at")]
        public DateTime? VerifiedAt { get; set; }

        [Column("average_rating")]
        public decimal? AverageRating { get; set; }

        [Column("total_reviews")]
        public int? TotalReviews { get; set; }

        [Column("hidden_gems_urls")]
        public List<string>? HiddenGemsUrls { get; set; }

        [Column("cover_photo_url")]
        public string? CoverPhotoUrl { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
