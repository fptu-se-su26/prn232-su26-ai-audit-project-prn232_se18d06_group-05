using Postgrest.Attributes;
using Postgrest.Models;

namespace TripMate_Webapi.Entities
{
    [Table("bookings")]
    public class BookingEntity : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Column("traveler_id")]
        public string TravelerId { get; set; }

        [Column("guide_profile_id")]
        public string GuideProfileId { get; set; }

        [Column("experience_package_id")]
        public string ExperiencePackageId { get; set; }

        [Column("booking_date")]
        public DateTime BookingDate { get; set; }

        [Column("start_time")]
        public DateTime StartTime { get; set; } 

        [Column("guest_count")]
        public int GuestCount { get; set; } = 1;

        [Column("total_amount")]
        public decimal TotalAmount { get; set; }

        [Column("platform_fee")]
        public decimal PlatformFee { get; set; }

        [Column("guide_earnings")]
        public decimal GuideEarnings { get; set; }

        [Column("status")]
        public int Status { get; set; } = 0; // 0=Pending, 1=Confirmed, 2=Completed, 3=Cancelled

        [Column("payment_reference")]
        public string? PaymentReference { get; set; }

        [Column("payment_method")]
        public string? PaymentMethod { get; set; }

        [Column("escrow_released")]
        public bool EscrowReleased { get; set; } = false;

        [Column("traveler_notes")]
        public string? TravelerNotes { get; set; }

        [Column("guide_response_at")]
        public DateTime? GuideResponseAt { get; set; }

        [Column("cancel_reason")]
        public string? CancelReason { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Reference(typeof(ProfileEntity))]
        public ProfileEntity? Traveler { get; set; }

        [Reference(typeof(GuideProfileEntity))]
        public GuideProfileEntity? GuideProfile { get; set; }

        [Reference(typeof(ExperiencePackageEntity))]
        public ExperiencePackageEntity? ExperiencePackage { get; set; }
    }
}
