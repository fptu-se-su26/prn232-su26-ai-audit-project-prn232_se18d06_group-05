using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TripMateWeb.Models
{
    [Table("reviews")]
    public class Review
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("guide_tour_id")]
        [Display(Name = "Tour")]
        public Guid GuideTourId { get; set; }

        [Required]
        [Column("user_id")]
        [Display(Name = "Người đánh giá")]
        public Guid UserId { get; set; }

        [Column("booking_id")]
        [Display(Name = "Booking")]
        public Guid? BookingId { get; set; }

        [Required]
        [Column("rating")]
        [Display(Name = "Đánh giá")]
        [Range(1, 5, ErrorMessage = "Đánh giá phải từ 1-5 sao")]
        public int Rating { get; set; }

        [Column("comment")]
        [Display(Name = "Bình luận")]
        [StringLength(1000)]
        public string? Comment { get; set; }

        [Column("created_at")]
        [Display(Name = "Ngày đánh giá")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("GuideTourId")]
        public virtual GuideTour GuideTour { get; set; } = null!;

        [ForeignKey("UserId")]
        public virtual Profile User { get; set; } = null!;

        [ForeignKey("BookingId")]
        public virtual Booking? Booking { get; set; }

        // Helper properties
        public string RatingStars => new string('★', Rating) + new string('☆', 5 - Rating);
        
        public string RatingClass => Rating switch
        {
            5 => "text-success",
            4 => "text-info",
            3 => "text-warning",
            2 => "text-orange",
            1 => "text-danger",
            _ => "text-muted"
        };
    }
}