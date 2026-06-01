using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TripMateWeb.Models
{
    [Table("conversations")]
    public class Conversation
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("traveler_id")]
        [Display(Name = "Du khách")]
        public Guid TravelerId { get; set; }

        [Required]
        [Column("guide_id")]
        [Display(Name = "Hướng dẫn viên")]
        public Guid GuideId { get; set; }

        [Column("booking_id")]
        [Display(Name = "Booking")]
        public Guid? BookingId { get; set; }

        [Column("created_at")]
        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("TravelerId")]
        public virtual Profile Traveler { get; set; } = null!;

        [ForeignKey("GuideId")]
        public virtual Profile Guide { get; set; } = null!;

        [ForeignKey("BookingId")]
        public virtual Booking? Booking { get; set; }

        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

        // Helper properties
        public Message? LastMessage => Messages.OrderByDescending(m => m.CreatedAt).FirstOrDefault();
        public int UnreadCount => Messages.Count(m => !m.IsRead);
        public bool HasUnreadMessages => UnreadCount > 0;
    }
}