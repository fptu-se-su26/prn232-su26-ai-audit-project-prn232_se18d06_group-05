using Postgrest.Attributes;
using Postgrest.Models;

namespace TripMate_Webapi.Entities
{
    [Table("profiles")]
    public class ProfileEntity : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; }

        [Column("email")]
        public string Email { get; set; }

        [Column("full_name")]
        public string FullName { get; set; }

        [Column("role")]
        public string Role { get; set; }

        [Column("avatar_url")]
        public string AvatarUrl { get; set; }

        [Column("mbti")]
        public string Mbti { get; set; }

        [Column("phone")]
        public string Phone { get; set; }

        [Column("status")]
        public string Status { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}
