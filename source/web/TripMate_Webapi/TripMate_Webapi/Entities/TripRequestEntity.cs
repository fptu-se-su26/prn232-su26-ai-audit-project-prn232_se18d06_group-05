using Postgrest.Attributes;
using Postgrest.Models;

namespace TripMate_Webapi.Entities
{
    [Table("trip_requests")]
    public class TripRequestEntity : BaseModel
    {
        [PrimaryKey("id", true)]
        public string Id { get; set; }

        [Column("traveler_id")]
        public string TravelerId { get; set; }

        [Column("destination")]
        public string Destination { get; set; }

        [Column("start_date")]
        public DateTime StartDate { get; set; }

        [Column("end_date")]
        public DateTime EndDate { get; set; }

        [Column("group_size")]
        public int GroupSize { get; set; }

        [Column("budget")]
        public string Budget { get; set; }

        [Column("notes")]
        public string Notes { get; set; }

        [Column("status")]
        public string Status { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
