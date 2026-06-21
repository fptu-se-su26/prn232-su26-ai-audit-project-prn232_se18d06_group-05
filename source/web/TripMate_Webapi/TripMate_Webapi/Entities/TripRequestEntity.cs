using Postgrest.Attributes;
using Postgrest.Models;

namespace TripMate_Webapi.Entities
{
    [Table("trip_requests")]
    public class TripRequestEntity : BaseModel
    {
        [PrimaryKey("id", true)]
        public string Id { get; set; } = string.Empty;

        [Column("traveler_id")]
        public string TravelerId { get; set; } = string.Empty;

        [Column("destination")]
        public string Destination { get; set; } = string.Empty;

        [Column("start_date")]
        public DateTime StartDate { get; set; }

        [Column("end_date")]
        public DateTime EndDate { get; set; }

        [Column("group_size")]
        public int GroupSize { get; set; }

        [Column("budget")]
        public string Budget { get; set; } = string.Empty;

        [Column("notes")]
        public string Notes { get; set; } = string.Empty;

        [Column("status")]
        public string Status { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
