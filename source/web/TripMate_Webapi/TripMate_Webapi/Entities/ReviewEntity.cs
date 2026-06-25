using Postgrest.Attributes;
using Postgrest.Models;

namespace TripMate_Webapi.Entities
{
    /// <summary>
    /// Maps to public.reviews table.
    /// M5: Cho phép Traveler đánh giá Guide sau khi booking hoàn tất (Status = 2).
    /// </summary>
    [Table("reviews")]
    public class ReviewEntity : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Column("booking_id")]
        public string BookingId { get; set; } = string.Empty;

        [Column("traveler_id")]
        public string TravelerId { get; set; } = string.Empty;

        [Column("guide_profile_id")]
        public string GuideProfileId { get; set; } = string.Empty;

        [Column("rating")]
        public int Rating { get; set; }

        [Column("comment")]
        public string? Comment { get; set; }

        [Column("is_visible")]
        public bool IsVisible { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
