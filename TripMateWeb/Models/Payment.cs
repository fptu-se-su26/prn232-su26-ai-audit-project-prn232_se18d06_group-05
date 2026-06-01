using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TripMateWeb.Models
{
    [Table("payments")]
    public class Payment
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("booking_id")]
        [Display(Name = "Booking")]
        public Guid BookingId { get; set; }

        [Required]
        [Column("amount")]
        [Display(Name = "Số tiền")]
        [DataType(DataType.Currency)]
        public decimal Amount { get; set; }

        [Required]
        [Column("payment_method")]
        [Display(Name = "Phương thức thanh toán")]
        [StringLength(50)]
        public string PaymentMethod { get; set; } = string.Empty;

        [Column("status")]
        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = "pending";

        [Column("created_at")]
        [Display(Name = "Ngày thanh toán")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("BookingId")]
        public virtual Booking Booking { get; set; } = null!;

        // Helper properties
        public bool IsPending => Status == "pending";
        public bool IsCompleted => Status == "completed";
        public bool IsFailed => Status == "failed";

        public string StatusDisplayName => Status switch
        {
            "pending" => "Chờ thanh toán",
            "completed" => "Đã thanh toán",
            "failed" => "Thanh toán thất bại",
            _ => "Không xác định"
        };

        public string PaymentMethodDisplayName => PaymentMethod switch
        {
            "credit_card" => "Thẻ tín dụng",
            "bank_transfer" => "Chuyển khoản",
            "cash" => "Tiền mặt",
            "e_wallet" => "Ví điện tử",
            _ => PaymentMethod
        };
    }
}