using Postgrest.Attributes;
using Postgrest.Models;

namespace TripMate_Webapi.Entities
{
    [Table("profiles")]
    public class ProfileEntity : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; } = string.Empty;

        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Column("full_name")]
        public string FullName { get; set; } = string.Empty;

        [Column("role")]
        public string Role { get; set; } = string.Empty;

        [Column("avatar_url")]
        public string AvatarUrl { get; set; } = string.Empty;


        [Column("phone_number")]
        public string? Phone { get; set; }

        [Column("location")]
        public string? Location { get; set; }

        [Column("average_rating")]
        public decimal? AverageRating { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
