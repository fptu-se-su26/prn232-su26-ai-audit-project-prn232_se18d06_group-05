using Postgrest.Attributes;
using Postgrest.Models;

namespace TripMate_Webapi.Entities
{
    [Table("bookings")]
    public class BookingEntity : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; }

        [Column("traveler_id")]
        public string TravelerId { get; set; }

        [Column("guide_id")]
        public string GuideId { get; set; }

        [Column("trip_request_id")]
        public string TripRequestId { get; set; }

        [Column("start_date")]
        public DateTime StartDate { get; set; }

        [Column("end_date")]
        public DateTime EndDate { get; set; }

        [Column("total_amount")]
        public decimal TotalAmount { get; set; }

        [Column("payment_status")]
        public string PaymentStatus { get; set; }

        [Column("booking_status")]
        public string BookingStatus { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
