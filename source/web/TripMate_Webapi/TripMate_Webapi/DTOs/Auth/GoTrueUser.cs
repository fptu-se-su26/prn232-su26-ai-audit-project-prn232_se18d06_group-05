using System.Text.Json.Serialization;

namespace TripMate_WebAPI.DTOs.Auth;

internal class GoTrueUser
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("email")]
    public string? Email { get; set; }
}
