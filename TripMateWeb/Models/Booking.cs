using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TripMateWeb.Models
{
    [Table("bookings")]
    public class Booking
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("guide_tour_id")]
        [Display(Name = "Tour")]
        public Guid GuideTourId { get; set; }

        [Required]
        [Column("traveler_id")]
        [Display(Name = "Du khách")]
        public Guid TravelerId { get; set; }

        [Required]
        [Column("tour_date")]
        [Display(Name = "Ngày tour")]
        [DataType(DataType.Date)]
        public DateOnly TourDate { get; set; }

        [Required]
        [Column("guests")]
        [Display(Name = "Số khách")]
        [Range(1, 50, ErrorMessage = "Số khách phải từ 1-50")]
        public int Guests { get; set; } = 1;

        [Required]
        [Column("total_price")]
        [Display(Name = "Tổng tiền")]
        [DataType(DataType.Currency)]
        public decimal TotalPrice { get; set; }

        [Column("status")]
        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = "pending";

        [Column("created_at")]
        [Display(Name = "Ngày đặt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("GuideTourId")]
        public virtual GuideTour GuideTour { get; set; } = null!;

        [ForeignKey("TravelerId")]
        public virtual Profile Traveler { get; set; } = null!;

        public virtual Payment? Payment { get; set; }
        public virtual Review? Review { get; set; }
        public virtual Conversation? Conversation { get; set; }

        // Helper properties
        public bool IsPending => Status == "pending";
        public bool IsConfirmed => Status == "confirmed";
        public bool IsCompleted => Status == "completed";
        public bool IsCancelled => Status == "cancelled";

        public string StatusDisplayName => Status switch
        {
            "pending" => "Chờ xác nhận",
            "confirmed" => "Đã xác nhận",
            "completed" => "Hoàn thành",
            "cancelled" => "Đã hủy",
            _ => "Không xác định"
        };

        public string StatusBadgeClass => Status switch
        {
            "pending" => "badge bg-warning",
            "confirmed" => "badge bg-info",
            "completed" => "badge bg-success",
            "cancelled" => "badge bg-danger",
            _ => "badge bg-secondary"
        };
    }
}