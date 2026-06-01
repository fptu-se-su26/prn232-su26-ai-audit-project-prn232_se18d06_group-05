using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TripMate_WebAPI.Models;
using TripMate_WebAPI.Services;

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
        // Updated to use guide_tours joined with tour_templates
        var query = $"{_supabaseUrl}/rest/v1/guide_tours?status=eq.active&order=rating.desc&select=*,tour_templates(title,description,location,images,created_at),profiles(full_name)";

        if (!string.IsNullOrWhiteSpace(location))
            query += $"&tour_templates.location=ilike.*{Uri.EscapeDataString(location)}*";

        if (!string.IsNullOrWhiteSpace(search))
            query += $"&or=(tour_templates.title.ilike.*{Uri.EscapeDataString(search)}*,tour_templates.location.ilike.*{Uri.EscapeDataString(search)}*)";

        var rows = await GetAsync<List<GuideTourRow>>(query);
        return rows?.Select(MapGuideTourToTourRow).ToList() ?? [];
    }

    // ── GET /tours/{id} ───────────────────────────────────────────────────────

    public async Task<TourRow?> GetTourByIdAsync(string id)
    {
        var query = $"{_supabaseUrl}/rest/v1/guide_tours?id=eq.{id}&select=*,tour_templates(title,description,location,images,created_at),profiles(full_name)";
        var rows = await GetAsync<List<GuideTourRow>>(query);
        return rows?.FirstOrDefault() != null ? MapGuideTourToTourRow(rows.First()) : null;
    }

    // ── POST /tours ───────────────────────────────────────────────────────────

    public async Task<TourRow> CreateTourAsync(string guideId, CreateTourRequest req, string userToken)
    {
        // First create or find tour template
        var templateId = await CreateOrFindTourTemplateAsync(req, userToken);
        
        // Then create guide tour
        var body = new
        {
            tour_template_id = templateId,
            guide_id = guideId,
            price = req.Price,
            duration_hours = req.DurationHours,
            max_participants = req.MaxParticipants,
            status = "active",
        };

        var request = BuildRequest(HttpMethod.Post, $"{_supabaseUrl}/rest/v1/guide_tours", userToken);
        request.Headers.Add("Prefer", "return=representation");
        request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var response = await _http.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        EnsureSuccess(response, content);

        var rows = JsonSerializer.Deserialize<List<GuideTourRow>>(content, _json);
        var guideTour = rows?.FirstOrDefault() ?? throw new Exception("Tạo tour thất bại");
        
        // Get full tour data with template info
        return await GetTourByIdAsync(guideTour.Id!) ?? throw new Exception("Không thể lấy thông tin tour vừa tạo");
    }

    private async Task<string> CreateOrFindTourTemplateAsync(CreateTourRequest req, string userToken)
    {
        // Check if template exists
        var existingQuery = $"{_supabaseUrl}/rest/v1/tour_templates?title=eq.{Uri.EscapeDataString(req.Title)}&location=eq.{Uri.EscapeDataString(req.Location)}";
        var existing = await GetAsync<List<TourTemplateRow>>(existingQuery);
        
        if (existing?.Any() == true)
        {
            return existing.First().Id!;
        }

        // Create new template
        var templateBody = new
        {
            title = req.Title,
            description = req.Description,
            location = req.Location,
            images = req.Images ?? []
        };

        var request = BuildRequest(HttpMethod.Post, $"{_supabaseUrl}/rest/v1/tour_templates", userToken);
        request.Headers.Add("Prefer", "return=representation");
        request.Content = new StringContent(JsonSerializer.Serialize(templateBody), Encoding.UTF8, "application/json");

        var response = await _http.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        EnsureSuccess(response, content);

        var templates = JsonSerializer.Deserialize<List<TourTemplateRow>>(content, _json);
        return templates?.FirstOrDefault()?.Id ?? throw new Exception("Tạo template thất bại");
    }

    // ── PATCH /tours/{id} ─────────────────────────────────────────────────────

    public async Task<TourRow> UpdateTourAsync(string tourId, UpdateTourRequest req, string userToken)
    {
        // Get current guide tour to access template
        var currentTour = await GetTourByIdAsync(tourId);
        if (currentTour == null)
            throw new Exception("Tour không tồn tại");

        // Update guide tour fields
        var guideTourUpdates = new Dictionary<string, object?>();
        if (req.Price.HasValue) guideTourUpdates["price"] = req.Price;
        if (req.DurationHours.HasValue) guideTourUpdates["duration_hours"] = req.DurationHours;
        if (req.MaxParticipants.HasValue) guideTourUpdates["max_participants"] = req.MaxParticipants;
        if (req.Status != null) guideTourUpdates["status"] = req.Status;

        if (guideTourUpdates.Count > 0)
        {
            var request = BuildRequest(HttpMethod.Patch, $"{_supabaseUrl}/rest/v1/guide_tours?id=eq.{tourId}", userToken);
            request.Headers.Add("Prefer", "return=representation");
            request.Content = new StringContent(JsonSerializer.Serialize(guideTourUpdates), Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            EnsureSuccess(response, content);
        }

        // Update template fields if needed
        var templateUpdates = new Dictionary<string, object?>();
        if (req.Title != null) templateUpdates["title"] = req.Title;
        if (req.Description != null) templateUpdates["description"] = req.Description;
        if (req.Location != null) templateUpdates["location"] = req.Location;
        if (req.Images != null) templateUpdates["images"] = req.Images;

        if (templateUpdates.Count > 0)
        {
            // Get template ID from current tour
            var templateQuery = $"{_supabaseUrl}/rest/v1/guide_tours?id=eq.{tourId}&select=tour_template_id";
            var templateResponse = await GetAsync<List<dynamic>>(templateQuery);
            var templateId = templateResponse?.FirstOrDefault()?.GetProperty("tour_template_id").GetString();

            if (!string.IsNullOrEmpty(templateId))
            {
                var request = BuildRequest(HttpMethod.Patch, $"{_supabaseUrl}/rest/v1/tour_templates?id=eq.{templateId}", userToken);
                request.Content = new StringContent(JsonSerializer.Serialize(templateUpdates), Encoding.UTF8, "application/json");

                var response = await _http.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();
                EnsureSuccess(response, content);
            }
        }

        // Return updated tour
        return await GetTourByIdAsync(tourId) ?? throw new Exception("Cập nhật tour thất bại");
    }

    // ── DELETE /tours/{id} ────────────────────────────────────────────────────

    public async Task DeleteTourAsync(string tourId, string userToken)
    {
        var request = BuildRequest(HttpMethod.Delete, $"{_supabaseUrl}/rest/v1/guide_tours?id=eq.{tourId}", userToken);
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

    // ── Mapping methods ───────────────────────────────────────────────────────

    private static TourRow MapGuideTourToTourRow(GuideTourRow guideTour)
    {
        return new TourRow
        {
            Id = guideTour.Id,
            GuideId = guideTour.GuideId,
            Title = guideTour.TourTemplate?.Title,
            Description = guideTour.TourTemplate?.Description,
            Location = guideTour.TourTemplate?.Location,
            Price = guideTour.Price,
            DurationHours = guideTour.DurationHours,
            MaxParticipants = guideTour.MaxParticipants,
            Images = guideTour.TourTemplate?.Images,
            Rating = guideTour.Rating,
            TotalReviews = guideTour.TotalReviews,
            Status = guideTour.Status,
            CreatedAt = guideTour.TourTemplate?.CreatedAt ?? DateTime.UtcNow,
            UpdatedAt = guideTour.TourTemplate?.CreatedAt ?? DateTime.UtcNow
        };
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

// ── New database schema models ────────────────────────────────────────────────

public class GuideTourRow
{
    [JsonPropertyName("id")]                public string? Id { get; set; }
    [JsonPropertyName("tour_template_id")]  public string? TourTemplateId { get; set; }
    [JsonPropertyName("guide_id")]          public string? GuideId { get; set; }
    [JsonPropertyName("price")]             public double Price { get; set; }
    [JsonPropertyName("duration_hours")]    public int DurationHours { get; set; }
    [JsonPropertyName("max_participants")]  public int MaxParticipants { get; set; }
    [JsonPropertyName("status")]            public string? Status { get; set; }
    [JsonPropertyName("rating")]            public double Rating { get; set; }
    [JsonPropertyName("total_reviews")]     public int TotalReviews { get; set; }
    [JsonPropertyName("tour_templates")]    public TourTemplateRow? TourTemplate { get; set; }
    [JsonPropertyName("profiles")]          public ProfileRow? Profile { get; set; }
}

public class TourTemplateRow
{
    [JsonPropertyName("id")]          public string? Id { get; set; }
    [JsonPropertyName("title")]       public string? Title { get; set; }
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("location")]    public string? Location { get; set; }
    [JsonPropertyName("images")]      public List<string>? Images { get; set; }
    [JsonPropertyName("created_at")]  public DateTime CreatedAt { get; set; }
}
