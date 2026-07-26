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
        public string TravelerId { get; set; } = string.Empty;

        [Column("guide_profile_id")]
        public string GuideProfileId { get; set; } = string.Empty;

        [Column("experience_package_id")]
        public string ExperiencePackageId { get; set; } = string.Empty;

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

        [Column("amount_paid")]
        public decimal AmountPaid { get; set; } = 0;

        [Column("payment_status")]
        public string PaymentStatus { get; set; } = "unpaid";

        [Column("scheduled_start_at")]
        public DateTime? ScheduledStartAt { get; set; }

        [Column("scheduled_end_at")]
        public DateTime? ScheduledEndAt { get; set; }

        [Column("completion_state")]
        public string CompletionState { get; set; } = "not_started";

        [Column("guide_completed_at")]
        public DateTime? GuideCompletedAt { get; set; }

        [Column("traveler_completed_at")]
        public DateTime? TravelerCompletedAt { get; set; }

        [Column("traveler_confirmation_due_at")]
        public DateTime? TravelerConfirmationDueAt { get; set; }

        [Column("completion_disputed_at")]
        public DateTime? CompletionDisputedAt { get; set; }

        [Column("completion_dispute_reason")]
        public string? CompletionDisputeReason { get; set; }

        [Column("payout_status")]
        public string PayoutStatus { get; set; } = "held";

        [Column("payout_eligible_at")]
        public DateTime? PayoutEligibleAt { get; set; }

        [Column("payout_released_at")]
        public DateTime? PayoutReleasedAt { get; set; }

        [Column("payout_failure_reason")]
        public string? PayoutFailureReason { get; set; }

        [Column("escrow_released")]
        public bool EscrowReleased { get; set; }

        [Column("traveler_notes")]
        public string? TravelerNotes { get; set; }

        [Column("payment_reference")]
        public string? PaymentReference { get; set; }

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
