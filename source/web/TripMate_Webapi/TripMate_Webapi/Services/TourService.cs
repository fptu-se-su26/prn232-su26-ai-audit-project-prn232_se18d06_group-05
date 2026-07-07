using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using TripMate_WebAPI.DTOs.Auth;

namespace TripMate_WebAPI.Services;

/// <summary>
/// Manages experience_packages table via Supabase REST API.
/// Refactored to match database_setup.sql schema.
/// </summary>
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

    // ── GET all packages ──────────────────────────────────────────────────────

    public async Task<List<ExperiencePackageRow>> GetToursAsync(string? search = null)
    {
        var query = $"{_supabaseUrl}/rest/v1/experience_packages" +
                    $"?is_active=eq.true&order=created_at.desc" +
                    $"&select=*,guide_profiles(id,user_id,bio,city_area,average_rating,total_reviews,profiles(full_name))";

        if (!string.IsNullOrWhiteSpace(search))
            query += $"&or=(title.ilike.*{Uri.EscapeDataString(search)}*,description.ilike.*{Uri.EscapeDataString(search)}*)";

        var results = await GetAsync<List<ExperiencePackageRow>>(query) ?? new List<ExperiencePackageRow>();
        return results.Where(r => r.Id != "00000000-0000-0000-0000-000000000000").ToList();
    }

    // ── GET package by ID ─────────────────────────────────────────────────────

    public async Task<ExperiencePackageRow?> GetTourByIdAsync(string id)
    {
        var query = $"{_supabaseUrl}/rest/v1/experience_packages" +
                    $"?id=eq.{id}" +
                    $"&select=*,guide_profiles(id,user_id,bio,city_area,average_rating,total_reviews,profiles(full_name))";

        var rows = await GetAsync<List<ExperiencePackageRow>>(query);
        return rows?.FirstOrDefault();
    }

    // ── GET packages by guide ─────────────────────────────────────────────────

    public async Task<List<ExperiencePackageRow>> GetToursByGuideAsync(string guideProfileId)
    {
        var query = $"{_supabaseUrl}/rest/v1/experience_packages" +
                    $"?guide_profile_id=eq.{guideProfileId}" +
                    $"&order=created_at.desc" +
                    $"&select=*,guide_profiles(id,user_id,bio,city_area,average_rating,total_reviews,profiles(full_name))";

        var results = await GetAsync<List<ExperiencePackageRow>>(query) ?? new List<ExperiencePackageRow>();
        return results.Where(r => r.Id != "00000000-0000-0000-0000-000000000000").ToList();
    }

    // ── POST create package ───────────────────────────────────────────────────

    public async Task<ExperiencePackageRow> CreateTourAsync(
        string guideProfileId, CreateTourRequest req, string userToken)
    {
        var body = new
        {
            guide_profile_id = guideProfileId,
            title = req.Title,
            description = req.Description,
            duration_hours = req.DurationHours,
            price_per_session = req.PricePerSession,
            price_per_person = req.PricePerPerson,
            max_group_size = req.MaxGroupSize,
            included_items = req.IncludedItems ?? new List<string>(),
            tags = req.Tags ?? new List<string>(),
            is_active = true,
        };

        var request = BuildRequest(HttpMethod.Post,
            $"{_supabaseUrl}/rest/v1/experience_packages", userToken);
        request.Headers.Add("Prefer", "return=representation");
        request.Content = new StringContent(
            JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var response = await _http.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        EnsureSuccess(response, content);

        var rows = JsonSerializer.Deserialize<List<ExperiencePackageRow>>(content, _json);
        var created = rows?.FirstOrDefault() ?? throw new Exception("Tạo gói trải nghiệm thất bại");

        // Fetch full data with guide join
        return await GetTourByIdAsync(created.Id!) ?? created;
    }

    // ── PATCH update package ──────────────────────────────────────────────────

    public async Task<ExperiencePackageRow> UpdateTourAsync(
        string packageId, UpdateTourRequest req, string userToken)
    {
        var updates = new Dictionary<string, object?>();
        if (req.Title != null) updates["title"] = req.Title;
        if (req.Description != null) updates["description"] = req.Description;
        if (req.DurationHours.HasValue) updates["duration_hours"] = req.DurationHours;
        if (req.PricePerSession.HasValue) updates["price_per_session"] = req.PricePerSession;
        if (req.PricePerPerson.HasValue) updates["price_per_person"] = req.PricePerPerson;
        if (req.MaxGroupSize.HasValue) updates["max_group_size"] = req.MaxGroupSize;
        if (req.IncludedItems != null) updates["included_items"] = req.IncludedItems;
        if (req.Tags != null) updates["tags"] = req.Tags;
        if (req.IsActive.HasValue) updates["is_active"] = req.IsActive;

        if (updates.Count == 0)
            return await GetTourByIdAsync(packageId) ?? throw new Exception("Gói trải nghiệm không tồn tại");

        var request = BuildRequest(HttpMethod.Patch,
            $"{_supabaseUrl}/rest/v1/experience_packages?id=eq.{packageId}", userToken);
        request.Headers.Add("Prefer", "return=representation");
        request.Content = new StringContent(
            JsonSerializer.Serialize(updates), Encoding.UTF8, "application/json");

        var response = await _http.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        EnsureSuccess(response, content);

        return await GetTourByIdAsync(packageId)
            ?? throw new Exception("Cập nhật gói trải nghiệm thất bại");
    }

    // ── DELETE package ────────────────────────────────────────────────────────

    public async Task DeleteTourAsync(string packageId, string userToken)
    {
        var request = BuildRequest(HttpMethod.Delete,
            $"{_supabaseUrl}/rest/v1/experience_packages?id=eq.{packageId}", userToken);
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

    // ── Mapping ───────────────────────────────────────────────────────────────

    public static TourDto MapToDto(ExperiencePackageRow row)
    {
        var guideName = row.GuideProfile?.Profile?.FullName;

        return new TourDto(
            Id: row.Id ?? "",
            GuideProfileId: row.GuideProfileId ?? "",
            GuideName: guideName,
            Title: row.Title ?? "",
            Description: row.Description ?? "",
            DurationHours: row.DurationHours,
            PricePerSession: row.PricePerSession,
            PricePerPerson: row.PricePerPerson,
            MaxGroupSize: row.MaxGroupSize,
            IncludedItems: row.IncludedItems ?? [],
            Tags: row.Tags ?? [],
            IsActive: row.IsActive,
            CreatedAt: row.CreatedAt
        );
    }
}

// ── Row models matching database_setup.sql ────────────────────────────────────

/// <summary>
/// Maps to public.experience_packages table
/// </summary>
public class ExperiencePackageRow
{
    [JsonPropertyName("id")]                public string? Id { get; set; }
    [JsonPropertyName("guide_profile_id")]  public string? GuideProfileId { get; set; }
    [JsonPropertyName("title")]             public string? Title { get; set; }
    [JsonPropertyName("description")]       public string? Description { get; set; }
    [JsonPropertyName("duration_hours")]    public decimal DurationHours { get; set; }
    [JsonPropertyName("price_per_session")] public decimal PricePerSession { get; set; }
    [JsonPropertyName("price_per_person")]  public decimal? PricePerPerson { get; set; }
    [JsonPropertyName("max_group_size")]    public int MaxGroupSize { get; set; } = 6;
    [JsonPropertyName("included_items")]    public List<string>? IncludedItems { get; set; }
    [JsonPropertyName("tags")]              public List<string>? Tags { get; set; }
    [JsonPropertyName("is_active")]         public bool IsActive { get; set; } = true;
    [JsonPropertyName("created_at")]        public DateTime CreatedAt { get; set; }

    // Joined from guide_profiles
    [JsonPropertyName("guide_profiles")]    public GuideProfileRow? GuideProfile { get; set; }
}

/// <summary>
/// Maps to public.guide_profiles table
/// </summary>
public class GuideProfileRow
{
    [JsonPropertyName("id")]              public string? Id { get; set; }
    [JsonPropertyName("user_id")]         public string? UserId { get; set; }
    [JsonPropertyName("bio")]             public string? Bio { get; set; }
    [JsonPropertyName("languages")]       public List<string>? Languages { get; set; }
    [JsonPropertyName("specialties")]     public List<string>? Specialties { get; set; }
    [JsonPropertyName("city_area")]       public string? CityArea { get; set; }
    [JsonPropertyName("price_per_hour")]  public decimal PricePerHour { get; set; }
    [JsonPropertyName("is_verified")]     public bool IsVerified { get; set; }
    [JsonPropertyName("verified_at")]     public DateTime? VerifiedAt { get; set; }
    [JsonPropertyName("average_rating")]  public decimal AverageRating { get; set; }
    [JsonPropertyName("total_reviews")]   public int TotalReviews { get; set; }
    [JsonPropertyName("hidden_gems_urls")] public List<string>? HiddenGemsUrls { get; set; }
    [JsonPropertyName("cover_photo_url")] public string? CoverPhotoUrl { get; set; }
    [JsonPropertyName("created_at")]      public DateTime CreatedAt { get; set; }
    [JsonPropertyName("updated_at")]      public DateTime UpdatedAt { get; set; }

    // Joined from profiles
    [JsonPropertyName("profiles")]        public ProfileRow? Profile { get; set; }
}
