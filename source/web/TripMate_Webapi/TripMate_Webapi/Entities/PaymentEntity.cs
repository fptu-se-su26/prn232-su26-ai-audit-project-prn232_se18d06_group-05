using Postgrest.Attributes;
using Postgrest.Models;

namespace TripMate_Webapi.Entities
{
    /// <summary>
    /// Maps one provider payment attempt or completed installment to
    /// public.payments. BookingEntity.PaymentStatus is the aggregate summary.
    /// </summary>
    [Table("payments")]
    public class PaymentEntity : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Column("booking_id")]
        public string BookingId { get; set; } = string.Empty;

        [Column("payer_id")]
        public string PayerId { get; set; } = string.Empty;

        [Column("amount")]
        public decimal Amount { get; set; }

        [Column("currency")]
        public string Currency { get; set; } = "VND";

        [Column("payment_method")]
        public string PaymentMethod { get; set; } = "payos";

        [Column("installment_type")]
        public string InstallmentType { get; set; } = "legacy";

        [Column("status")]
        public string Status { get; set; } = "pending";

        [Column("provider_order_code")]
        public string? ProviderOrderCode { get; set; }

        [Column("provider_transaction_id")]
        public string? ProviderTransactionId { get; set; }

        [Column("payment_intent")]
        public string? PaymentIntent { get; set; }

        [Column("checkout_url")]
        public string? CheckoutUrl { get; set; }

        [Column("paid_at")]
        public DateTime? PaidAt { get; set; }

        [Column("expires_at")]
        public DateTime? ExpiresAt { get; set; }

        [Column("processed_at")]
        public DateTime? ProcessedAt { get; set; }

        [Column("failure_reason")]
        public string? FailureReason { get; set; }

        [Column("refunded_amount")]
        public decimal RefundedAmount { get; set; }

        [Column("idempotency_key")]
        public string? IdempotencyKey { get; set; }

        [Column("metadata")]
        public Dictionary<string, object?> Metadata { get; set; } = new();

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Reference(typeof(BookingEntity))]
        public BookingEntity? Booking { get; set; }

        [Reference(typeof(ProfileEntity))]
        public ProfileEntity? Payer { get; set; }
    }
}
