using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TripMateWeb.Models
{
    [Table("profiles")]
    public class Profile
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; }

        [Required]
        [Column("email")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Column("full_name")]
        [Display(Name = "Họ và tên")]
        public string? FullName { get; set; }

        [Column("phone")]
        [Display(Name = "Số điện thoại")]
        [Phone]
        public string? Phone { get; set; }

        [Column("avatar_url")]
        [Display(Name = "Ảnh đại diện")]
        public string? AvatarUrl { get; set; }

        [Column("role")]
        [Display(Name = "Vai trò")]
        public string Role { get; set; } = "traveler";

        [Column("created_at")]
        [Display(Name = "Ngày tạo")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        [Display(Name = "Ngày cập nhật")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<GuideTour> GuideTours { get; set; } = new List<GuideTour>();
        public virtual ICollection<Booking> TravelerBookings { get; set; } = new List<Booking>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
        public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
        public virtual ICollection<Conversation> TravelerConversations { get; set; } = new List<Conversation>();
        public virtual ICollection<Conversation> GuideConversations { get; set; } = new List<Conversation>();
        public virtual ICollection<GuideCertificate> GuideCertificates { get; set; } = new List<GuideCertificate>();

        // Helper properties
        public bool IsTraveler => Role == "traveler";
        public bool IsGuide => Role == "guide";
        public bool IsAdmin => Role == "admin";
    }

    public enum UserRole
    {
        Traveler,
        Guide,
        Admin
    }
}