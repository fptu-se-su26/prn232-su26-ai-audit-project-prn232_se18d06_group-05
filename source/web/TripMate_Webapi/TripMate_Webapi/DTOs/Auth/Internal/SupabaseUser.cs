using System.Text.Json.Serialization;

namespace TripMate_WebAPI.DTOs.Auth.Internal;

public class SupabaseUser
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("email_verified")]
    public bool EmailVerified { get; set; }

    [JsonPropertyName("user_metadata")]
    public UserMetadata? UserMetadata { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime? CreatedAt { get; set; }
}
