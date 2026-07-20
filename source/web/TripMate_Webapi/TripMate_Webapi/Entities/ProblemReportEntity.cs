using Postgrest.Attributes;
using Postgrest.Models;

namespace TripMate_Webapi.Entities
{
    [Table("problem_reports")]
    public class ProblemReportEntity : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Column("user_id")]
        public string UserId { get; set; } = string.Empty;

        [Column("type")]
        public string Type { get; set; } = string.Empty; // 'booking', 'payment', 'account', 'technical', 'other'

        [Column("booking_id")]
        public string? BookingId { get; set; }

        [Column("title")]
        public string Title { get; set; } = string.Empty;

        [Column("description")]
        public string Description { get; set; } = string.Empty;

        [Column("image_url")]
        public string? ImageUrl { get; set; }

        [Column("status")]
        public string Status { get; set; } = "pending"; // 'pending', 'resolved'

        [Column("admin_comment")]
        public string? AdminComment { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Reference(typeof(ProfileEntity))]
        public ProfileEntity? User { get; set; }

        [Reference(typeof(BookingEntity))]
        public BookingEntity? Booking { get; set; }
    }
}
