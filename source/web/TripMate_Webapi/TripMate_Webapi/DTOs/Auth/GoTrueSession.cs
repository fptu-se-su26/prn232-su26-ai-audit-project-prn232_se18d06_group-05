using System.Text.Json.Serialization;

namespace TripMate_WebAPI.DTOs.Auth;

internal class GoTrueSession
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = "";

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = "";

    [JsonPropertyName("expires_at")]
    public long ExpiresAt { get; set; }

    [JsonPropertyName("user")]
    public GoTrueUser User { get; set; } = new();
}
