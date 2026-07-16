using System.Text.Json.Serialization;

namespace TripMate_WebAPI.DTOs.Auth;

internal class GoTrueError
{
    // Supabase GoTrue v2 format: {"code":400,"error_code":"invalid_credentials","msg":"..."}
    [JsonPropertyName("msg")]
    public string? Msg { get; set; }

    [JsonPropertyName("error_code")]
    public string? ErrorCode { get; set; }

    [JsonPropertyName("code")]
    public System.Text.Json.JsonElement? Code { get; set; }

    // Legacy format: {"error":"...", "error_description":"..."}
    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; set; }

    // Helper to get most meaningful message
    public string GetMessage() =>
        ErrorDescription ?? Msg ?? Error ?? "Lỗi xác thực";
}
