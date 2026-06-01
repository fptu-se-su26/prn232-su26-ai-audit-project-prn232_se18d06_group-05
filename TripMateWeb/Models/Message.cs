using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TripMateWeb.Models
{
    [Table("messages")]
    public class Message
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("conversation_id")]
        [Display(Name = "Cuộc trò chuyện")]
        public Guid ConversationId { get; set; }

        [Required]
        [Column("sender_id")]
        [Display(Name = "Người gửi")]
        public Guid SenderId { get; set; }

        [Required]
        [Column("content")]
        [Display(Name = "Nội dung")]
        [StringLength(1000)]
        public string Content { get; set; } = string.Empty;

        [Column("is_read")]
        [Display(Name = "Đã đọc")]
        public bool IsRead { get; set; } = false;

        [Column("created_at")]
        [Display(Name = "Thời gian gửi")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("ConversationId")]
        public virtual Conversation Conversation { get; set; } = null!;

        [ForeignKey("SenderId")]
        public virtual Profile Sender { get; set; } = null!;

        // Helper properties
        public string TimeAgo
        {
            get
            {
                var timeSpan = DateTime.UtcNow - CreatedAt;
                return timeSpan.TotalMinutes < 1 ? "Vừa xong" :
                       timeSpan.TotalMinutes < 60 ? $"{(int)timeSpan.TotalMinutes} phút trước" :
                       timeSpan.TotalHours < 24 ? $"{(int)timeSpan.TotalHours} giờ trước" :
                       $"{(int)timeSpan.TotalDays} ngày trước";
            }
        }
    }
}