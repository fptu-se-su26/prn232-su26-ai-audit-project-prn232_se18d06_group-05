using System.Text.Json.Serialization;

namespace TripMate_WebAPI.DTOs.Auth.Internal;

public class GoogleTokenInfo
{
    [JsonPropertyName("iss")]
    public string? Issuer { get; set; }

    [JsonPropertyName("aud")]
    public string? Audience { get; set; }

    [JsonPropertyName("sub")]
    public string? Subject { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("email_verified")]
    public string? EmailVerified { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("picture")]
    public string? Picture { get; set; }

    [JsonPropertyName("given_name")]
    public string? GivenName { get; set; }

    [JsonPropertyName("family_name")]
    public string? FamilyName { get; set; }

    [JsonPropertyName("exp")]
    public long Expiration { get; set; }
}
