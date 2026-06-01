using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TripMateWeb.Models
{
    [Table("guide_tours")]
    public class GuideTour
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("tour_template_id")]
        [Display(Name = "Mẫu tour")]
        public Guid TourTemplateId { get; set; }

        [Required]
        [Column("guide_id")]
        [Display(Name = "Hướng dẫn viên")]
        public Guid GuideId { get; set; }

        [Required]
        [Column("price")]
        [Display(Name = "Giá")]
        [Range(0, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0")]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }

        [Required]
        [Column("duration_hours")]
        [Display(Name = "Thời gian (giờ)")]
        [Range(1, 24, ErrorMessage = "Thời gian phải từ 1-24 giờ")]
        public int DurationHours { get; set; }

        [Column("max_participants")]
        [Display(Name = "Số người tối đa")]
        [Range(1, 50, ErrorMessage = "Số người phải từ 1-50")]
        public int MaxParticipants { get; set; } = 10;

        [Column("status")]
        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = "active";

        [Column("rating")]
        [Display(Name = "Đánh giá")]
        [Range(0, 5)]
        public decimal Rating { get; set; } = 0;

        [Column("total_reviews")]
        [Display(Name = "Tổng số đánh giá")]
        public int TotalReviews { get; set; } = 0;

        // Navigation properties
        [ForeignKey("TourTemplateId")]
        public virtual TourTemplate TourTemplate { get; set; } = null!;

        [ForeignKey("GuideId")]
        public virtual Profile Guide { get; set; } = null!;

        public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public virtual ICollection<TourAvailability> Availabilities { get; set; } = new List<TourAvailability>();

        // Helper properties
        public bool IsActive => Status == "active";
        public bool IsInactive => Status == "inactive";
        public string DisplayTitle => TourTemplate?.Title ?? "Unknown Tour";
        public string DisplayLocation => TourTemplate?.Location ?? "Unknown Location";
    }
}