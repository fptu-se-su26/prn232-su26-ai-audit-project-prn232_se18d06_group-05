using Postgrest.Attributes;
using Postgrest.Models;

namespace TripMate_Webapi.Entities
{
    [Table("guide_availability")]
    public class GuideAvailabilityEntity : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Column("guide_profile_id")]
        public string GuideProfileId { get; set; } = string.Empty;

        [Column("unavailable_date")]
        public string UnavailableDate { get; set; } = string.Empty; // "yyyy-MM-dd"

        [Column("reason")]
        public string? Reason { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
