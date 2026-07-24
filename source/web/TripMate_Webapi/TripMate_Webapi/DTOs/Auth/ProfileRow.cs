using System.Text.Json.Serialization;

namespace TripMate_WebAPI.DTOs.Auth;

/// <summary>
/// Maps to public.profiles table in database_setup.sql
/// </summary>
public class ProfileRow
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("full_name")]
    public string? FullName { get; set; }

    [JsonPropertyName("phone_number")]
    public string? PhoneNumber { get; set; }

    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; set; }

    [JsonPropertyName("role")]
    public string? Role { get; set; }

    [JsonPropertyName("is_active")]
    public bool IsActive { get; set; } = true;

    [JsonPropertyName("bio")]
    public string? Bio { get; set; }

    [JsonPropertyName("languages")]
    public string? Languages { get; set; }

    [JsonPropertyName("experience")]
    public string? Experience { get; set; }

    [JsonPropertyName("specialization")]
    public string? Specialization { get; set; }

    [JsonPropertyName("certificate_url")]
    public string? CertificateUrl { get; set; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
