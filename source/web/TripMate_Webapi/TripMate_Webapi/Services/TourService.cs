using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TripMate_WebAPI.Models;

namespace TripMate_WebAPI.Services;

/// Gọi Supabase REST API để thao tác bảng tours
public class TourService
{
    private readonly HttpClient _http;
    private readonly string _supabaseUrl;
    private readonly string _anonKey;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public TourService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _supabaseUrl = config["Supabase:Url"]!;
        _anonKey = config["Supabase:AnonKey"]!;
    }

    // ── GET /tours ────────────────────────────────────────────────────────────

    public async Task<List<TourRow>> GetToursAsync(string? location = null, string? search = null)
    {
        var query = $"{_supabaseUrl}/rest/v1/tours?status=eq.active&order=created_at.desc&select=*";

        if (!string.IsNullOrWhiteSpace(location))
            query += $"&location=ilike.*{Uri.EscapeDataString(location)}*";

        if (!string.IsNullOrWhiteSpace(search))
            query += $"&or=(title.ilike.*{Uri.EscapeDataString(search)}*,location.ilike.*{Uri.EscapeDataString(search)}*)";

        var rows = await GetAsync<List<TourRow>>(query);
        return rows ?? [];
    }

    // ── GET /tours/{id} ───────────────────────────────────────────────────────

    public async Task<TourRow?> GetTourByIdAsync(string id)
    {
        var query = $"{_supabaseUrl}/rest/v1/tours?id=eq.{id}&select=*";
        var rows = await GetAsync<List<TourRow>>(query);
        return rows?.FirstOrDefault();
    }

    // ── POST /tours ───────────────────────────────────────────────────────────

    public async Task<TourRow> CreateTourAsync(string guideId, CreateTourRequest req, string userToken)
    {
        var body = new
        {
            guide_id = guideId,
            title = req.Title,
            description = req.Description,
            location = req.Location,
            price = req.Price,
            duration_hours = req.DurationHours,
            max_participants = req.MaxParticipants,
            images = req.Images ?? [],
            status = "active",
        };

        var request = BuildRequest(HttpMethod.Post, $"{_supabaseUrl}/rest/v1/tours", userToken);
        request.Headers.Add("Prefer", "return=representation");
        request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var response = await _http.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        EnsureSuccess(response, content);

        var rows = JsonSerializer.Deserialize<List<TourRow>>(content, _json);
        return rows?.FirstOrDefault() ?? throw new Exception("Tạo tour thất bại");
    }

    // ── PATCH /tours/{id} ─────────────────────────────────────────────────────

    public async Task<TourRow> UpdateTourAsync(string tourId, UpdateTourRequest req, string userToken)
    {
        var updates = new Dictionary<string, object?>();
        if (req.Title != null) updates["title"] = req.Title;
        if (req.Description != null) updates["description"] = req.Description;
        if (req.Location != null) updates["location"] = req.Location;
        if (req.Price.HasValue) updates["price"] = req.Price;
        if (req.DurationHours.HasValue) updates["duration_hours"] = req.DurationHours;
        if (req.MaxParticipants.HasValue) updates["max_participants"] = req.MaxParticipants;
        if (req.Images != null) updates["images"] = req.Images;
        if (req.Status != null) updates["status"] = req.Status;
        updates["updated_at"] = DateTime.UtcNow;

        var request = BuildRequest(HttpMethod.Patch, $"{_supabaseUrl}/rest/v1/tours?id=eq.{tourId}", userToken);
        request.Headers.Add("Prefer", "return=representation");
        request.Content = new StringContent(JsonSerializer.Serialize(updates), Encoding.UTF8, "application/json");

        var response = await _http.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        EnsureSuccess(response, content);

        var rows = JsonSerializer.Deserialize<List<TourRow>>(content, _json);
        return rows?.FirstOrDefault() ?? throw new Exception("Cập nhật tour thất bại");
    }

    // ── DELETE /tours/{id} ────────────────────────────────────────────────────

    public async Task DeleteTourAsync(string tourId, string userToken)
    {
        var request = BuildRequest(HttpMethod.Delete, $"{_supabaseUrl}/rest/v1/tours?id=eq.{tourId}", userToken);
        var response = await _http.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        EnsureSuccess(response, content);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private async Task<T?> GetAsync<T>(string url)
    {
        var request = BuildRequest(HttpMethod.Get, url);
        var response = await _http.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        EnsureSuccess(response, content);
        return JsonSerializer.Deserialize<T>(content, _json);
    }

    private HttpRequestMessage BuildRequest(HttpMethod method, string url, string? userToken = null)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("apikey", _anonKey);
        // Dùng JWT của user cho write ops để RLS nhận diện được auth.uid()
        // Dùng anonKey cho read ops (public)
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer", userToken ?? _anonKey);
        request.Headers.Add("Accept", "application/json");
        return request;
    }

    private static void EnsureSuccess(HttpResponseMessage response, string content)
    {
        if (!response.IsSuccessStatusCode)
            throw new Exception($"Supabase error {response.StatusCode}: {content}");
    }

    public static TourDto MapToDto(TourRow row) => new(
        Id: row.Id ?? "",
        GuideId: row.GuideId ?? "",
        Title: row.Title ?? "",
        Description: row.Description,
        Location: row.Location ?? "",
        Price: row.Price,
        DurationHours: row.DurationHours,
        MaxParticipants: row.MaxParticipants,
        Images: row.Images ?? [],
        Rating: row.Rating,
        TotalReviews: row.TotalReviews,
        Status: row.Status ?? "active",
        CreatedAt: row.CreatedAt,
        UpdatedAt: row.UpdatedAt
    );
}

// ── Supabase row model ────────────────────────────────────────────────────────

public class TourRow
{
    [JsonPropertyName("id")]          public string? Id { get; set; }
    [JsonPropertyName("guide_id")]    public string? GuideId { get; set; }
    [JsonPropertyName("title")]       public string? Title { get; set; }
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("location")]    public string? Location { get; set; }
    [JsonPropertyName("price")]       public double Price { get; set; }
    [JsonPropertyName("duration_hours")]   public int DurationHours { get; set; }
    [JsonPropertyName("max_participants")] public int MaxParticipants { get; set; }
    [JsonPropertyName("images")]      public List<string>? Images { get; set; }
    [JsonPropertyName("rating")]      public double Rating { get; set; }
    [JsonPropertyName("total_reviews")]    public int TotalReviews { get; set; }
    [JsonPropertyName("status")]      public string? Status { get; set; }
    [JsonPropertyName("created_at")]  public DateTime CreatedAt { get; set; }
    [JsonPropertyName("updated_at")]  public DateTime UpdatedAt { get; set; }
}
