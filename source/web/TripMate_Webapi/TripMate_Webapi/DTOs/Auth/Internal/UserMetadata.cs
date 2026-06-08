using System.Text.Json.Serialization;

namespace TripMate_WebAPI.DTOs.Auth.Internal;

public class UserMetadata
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("picture")]
    public string? Picture { get; set; }

    [JsonPropertyName("avatar_url")]
    public string? AvatarUrl { get; set; }

    [JsonPropertyName("full_name")]
    public string? FullName { get; set; }

    [JsonPropertyName("provider_id")]
    public string? ProviderId { get; set; }

    [JsonPropertyName("sub")]
    public string? Sub { get; set; }
}
