using Postgrest.Attributes;
using Postgrest.Models;

namespace TripMate_Webapi.Entities
{
    [Table("saved_guides")]
    public class SavedGuideEntity : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Column("traveler_id")]
        public string TravelerId { get; set; } = string.Empty;

        [Column("guide_profile_id")]
        public string GuideProfileId { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Reference(typeof(GuideProfileEntity))]
        public GuideProfileEntity? GuideProfile { get; set; }
    }
}
