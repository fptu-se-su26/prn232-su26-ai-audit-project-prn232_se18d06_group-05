using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TripMateWeb.Models
{
    [Table("tour_templates")]
    public class TourTemplate
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("title")]
        [Display(Name = "Tiêu đề")]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Column("description")]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }

        [Required]
        [Column("location")]
        [Display(Name = "Địa điểm")]
        [StringLength(100)]
        public string Location { get; set; } = string.Empty;

        [Column("images")]
        [Display(Name = "Hình ảnh")]
        public string[] Images { get; set; } = Array.Empty<string>();

        [Column("created_at")]
        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<GuideTour> GuideTours { get; set; } = new List<GuideTour>();
    }
}