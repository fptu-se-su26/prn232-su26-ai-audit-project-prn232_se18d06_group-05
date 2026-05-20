using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TripMate_WebAPI.Services;

public class NotificationService
{
    private readonly HttpClient _http;
    private readonly string _supabaseUrl;
    private readonly string _anonKey;

    public NotificationService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _supabaseUrl = config["Supabase:Url"]!;
        _anonKey = config["Supabase:AnonKey"]!;
    }

    public async Task SendAsync(string userId, string type, string title, string body,
        object? data = null)
    {
        var payload = new
        {
            user_id = userId,
            type,
            title,
            body,
            data = data != null ? JsonSerializer.SerializeToElement(data) : (object?)null,
        };

        var req = new HttpRequestMessage(HttpMethod.Post,
            $"{_supabaseUrl}/rest/v1/notifications");
        req.Headers.Add("apikey", _anonKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _anonKey);
        req.Headers.Add("Accept", "application/json");
        req.Content = new StringContent(
            JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        await _http.SendAsync(req); // fire-and-forget, không throw
    }

    public async Task<List<NotificationDto>> GetMyNotificationsAsync(
        string userId, string userToken)
    {
        var url = $"{_supabaseUrl}/rest/v1/notifications" +
                  $"?user_id=eq.{userId}&order=created_at.desc&limit=50&select=*";

        var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("apikey", _anonKey);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
        req.Headers.Add("Accept", "application/json");

        var res = await _http.SendAsync(req);
        var content = await res.Content.ReadAsStringAsync();

        var rows = JsonSerializer.Deserialize<List<NotifRow>>(content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

        return rows.Select(r => new NotificationDto(
            r.Id ?? "", r.Type ?? "", r.Title ?? "", r.Body ?? "",
            r.IsRead, r.CreatedAt)).ToList();
    }
}

public record NotificationDto(string Id, string Type, string Title,
    string Body, bool IsRead, DateTime CreatedAt);

internal class NotifRow
{
    [JsonPropertyName("id")]         public string? Id { get; set; }
    [JsonPropertyName("type")]       public string? Type { get; set; }
    [JsonPropertyName("title")]      public string? Title { get; set; }
    [JsonPropertyName("body")]       public string? Body { get; set; }
    [JsonPropertyName("is_read")]    public bool IsRead { get; set; }
    [JsonPropertyName("created_at")] public DateTime CreatedAt { get; set; }
}
