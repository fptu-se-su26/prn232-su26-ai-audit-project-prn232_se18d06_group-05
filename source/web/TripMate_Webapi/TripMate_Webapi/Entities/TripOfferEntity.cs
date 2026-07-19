using Postgrest.Attributes;
using Postgrest.Models;

namespace TripMate_Webapi.Entities
{
    [Table("trip_offers")]
    public class TripOfferEntity : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = string.Empty;

        [Column("trip_request_id")]
        public string TripRequestId { get; set; } = string.Empty;

        [Column("guide_profile_id")]
        public string GuideProfileId { get; set; } = string.Empty;

        [Column("message")]
        public string Message { get; set; } = string.Empty;

        [Column("proposed_price")]
        public decimal ProposedPrice { get; set; }

        [Column("status")]
        public string Status { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
