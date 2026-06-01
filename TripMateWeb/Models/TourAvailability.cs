using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TripMateWeb.Models
{
    [Table("tour_availability")]
    public class TourAvailability
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("guide_tour_id")]
        [Display(Name = "Tour")]
        public Guid GuideTourId { get; set; }

        [Required]
        [Column("date")]
        [Display(Name = "Ngày")]
        [DataType(DataType.Date)]
        public DateOnly Date { get; set; }

        [Required]
        [Column("remaining_slots")]
        [Display(Name = "Chỗ còn lại")]
        [Range(0, int.MaxValue)]
        public int RemainingSlots { get; set; }

        // Navigation properties
        [ForeignKey("GuideTourId")]
        public virtual GuideTour GuideTour { get; set; } = null!;

        // Helper properties
        public bool IsAvailable => RemainingSlots > 0;
        public bool IsFull => RemainingSlots == 0;
        
        public string AvailabilityStatus => RemainingSlots switch
        {
            0 => "Hết chỗ",
            1 => "Còn 1 chỗ",
            _ => $"Còn {RemainingSlots} chỗ"
        };

        public string AvailabilityClass => RemainingSlots switch
        {
            0 => "text-danger",
            1 => "text-warning",
            _ => "text-success"
        };
    }
}