using System.Text.Json;
using System.Text.Json.Serialization;

namespace TripMate_WebAPI.Services;

/// Dùng SerpAPI Google Local để tìm địa điểm xung quanh
public class LocationService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;

    public LocationService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _apiKey = config["SerpApi:ApiKey"]!;
    }

    public async Task<List<LocalPlace>> SearchNearbyAsync(
        string query, string location, string hl = "vi", string gl = "vn")
    {
        var url = $"https://serpapi.com/search.json" +
                  $"?engine=google_local" +
                  $"&q={Uri.EscapeDataString(query)}" +
                  $"&location={Uri.EscapeDataString(location)}" +
                  $"&hl={hl}&gl={gl}" +
                  $"&api_key={_apiKey}";

        var response = await _http.GetAsync(url);
        var content  = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"SerpAPI error: {content}");

        var result = JsonSerializer.Deserialize<SerpApiResponse>(content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return result?.LocalResults?.Select(r => new LocalPlace(
            Title:   r.Title ?? "",
            Address: r.Address ?? "",
            Rating:  r.Rating,
            Reviews: r.Reviews,
            Type:    r.Type ?? "",
            Gps:     r.Gps != null ? new GpsCoord(r.Gps.Latitude, r.Gps.Longitude) : null
        )).ToList() ?? [];
    }
}

// ── DTOs ──────────────────────────────────────────────────────────────────────

public record LocalPlace(
    string Title, string Address, double? Rating,
    int? Reviews, string Type, GpsCoord? Gps);

public record GpsCoord(double Latitude, double Longitude);

internal class SerpApiResponse
{
    [JsonPropertyName("local_results")]
    public List<SerpLocalResult>? LocalResults { get; set; }
}

internal class SerpLocalResult
{
    [JsonPropertyName("title")]   public string? Title { get; set; }
    [JsonPropertyName("address")] public string? Address { get; set; }
    [JsonPropertyName("rating")]  public double? Rating { get; set; }
    [JsonPropertyName("reviews")] public int? Reviews { get; set; }
    [JsonPropertyName("type")]    public string? Type { get; set; }
    [JsonPropertyName("gps_coordinates")] public SerpGps? Gps { get; set; }
}

internal class SerpGps
{
    [JsonPropertyName("latitude")]  public double Latitude { get; set; }
    [JsonPropertyName("longitude")] public double Longitude { get; set; }
}
