using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TripMateWeb.Models
{
    [Table("guide_certificates")]
    public class GuideCertificate
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("guide_id")]
        [Display(Name = "Hướng dẫn viên")]
        public Guid GuideId { get; set; }

        [Required]
        [Column("certificate_name")]
        [Display(Name = "Tên chứng chỉ")]
        [StringLength(200)]
        public string CertificateName { get; set; } = string.Empty;

        [Required]
        [Column("file_url")]
        [Display(Name = "File chứng chỉ")]
        public string FileUrl { get; set; } = string.Empty;

        [Column("status")]
        [Display(Name = "Trạng thái")]
        public string Status { get; set; } = "pending";

        [Column("created_at")]
        [Display(Name = "Ngày nộp")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("GuideId")]
        public virtual Profile Guide { get; set; } = null!;

        // Helper properties
        public bool IsPending => Status == "pending";
        public bool IsVerified => Status == "verified";
        public bool IsRejected => Status == "rejected";

        public string StatusDisplayName => Status switch
        {
            "pending" => "Chờ xét duyệt",
            "verified" => "Đã xác minh",
            "rejected" => "Bị từ chối",
            _ => "Không xác định"
        };

        public string StatusBadgeClass => Status switch
        {
            "pending" => "badge bg-warning",
            "verified" => "badge bg-success",
            "rejected" => "badge bg-danger",
            _ => "badge bg-secondary"
        };
    }
}