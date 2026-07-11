using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TripMate_Webapi.Entities;

namespace TripMate_WebAPI.Services
{
    public class ProblemReportService
    {
        private readonly HttpClient _http;
        private readonly string _supabaseUrl;
        private readonly string _anonKey;
        private readonly string _serviceRoleKey;
        private readonly ILogger<ProblemReportService> _logger;

        private static readonly JsonSerializerOptions _json = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        public ProblemReportService(HttpClient http, IConfiguration config, ILogger<ProblemReportService> logger)
        {
            _http = http;
            _supabaseUrl = config["Supabase:Url"]!;
            _anonKey = config["Supabase:AnonKey"]!;
            _serviceRoleKey = config["Supabase:ServiceRoleKey"]!;
            _logger = logger;
        }

        private HttpRequestMessage BuildRequest(HttpMethod method, string url, string token)
        {
            var req = new HttpRequestMessage(method, url);
            req.Headers.Add("apikey", _anonKey);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            req.Headers.Add("Accept", "application/json");
            return req;
        }

        private HttpRequestMessage BuildAdminRequest(HttpMethod method, string url)
        {
            var req = new HttpRequestMessage(method, url);
            req.Headers.Add("apikey", _serviceRoleKey);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _serviceRoleKey);
            req.Headers.Add("Accept", "application/json");
            return req;
        }

        private static void EnsureSuccess(HttpResponseMessage r, string content)
        {
            if (!r.IsSuccessStatusCode)
                throw new Exception($"Supabase Service error {r.StatusCode}: {content}");
        }

        // Create a new problem report
        public async Task<ProblemReportEntity> CreateReportAsync(string userId, string type, string? bookingId, string title, string description, string? imageUrl, string userToken)
        {
            var body = new
            {
                user_id = userId,
                type = type,
                booking_id = string.IsNullOrEmpty(bookingId) ? null : bookingId,
                title = title,
                description = description,
                image_url = imageUrl,
                status = "pending"
            };

            var url = $"{_supabaseUrl}/rest/v1/problem_reports";
            var request = BuildRequest(HttpMethod.Post, url, userToken);
            request.Headers.Add("Prefer", "return=representation");
            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            EnsureSuccess(response, content);

            var createdList = JsonSerializer.Deserialize<List<ProblemReportEntity>>(content, _json);
            return createdList?.FirstOrDefault() ?? throw new Exception("Tạo báo cáo thất bại");
        }

        // Fetch reports for the logged-in user
        public async Task<List<ProblemReportEntity>> GetReportsByUserAsync(string userId, string userToken)
        {
            var url = $"{_supabaseUrl}/rest/v1/problem_reports?user_id=eq.{userId}&order=created_at.desc&select=*";
            var request = BuildRequest(HttpMethod.Get, url, userToken);
            var response = await _http.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            EnsureSuccess(response, content);

            return JsonSerializer.Deserialize<List<ProblemReportEntity>>(content, _json) ?? new();
        }

        // Fetch all reports
        public async Task<List<ProblemReportEntity>> GetAllReportsAsync()
        {
            var url = $"{_supabaseUrl}/rest/v1/problem_reports?select=*,profiles:user_id(full_name,email)&order=created_at.desc";
            var request = BuildAdminRequest(HttpMethod.Get, url);
            var response = await _http.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            EnsureSuccess(response, content);

            return JsonSerializer.Deserialize<List<ProblemReportEntity>>(content, _json) ?? new();
        }

        // Resolve report
        public async Task<bool> ResolveReportAsync(string reportId, string adminComment)
        {
            var body = new
            {
                status = "resolved",
                admin_comment = adminComment,
                updated_at = DateTime.UtcNow
            };

            var url = $"{_supabaseUrl}/rest/v1/problem_reports?id=eq.{reportId}";
            var request = BuildAdminRequest(HttpMethod.Patch, url);
            request.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            var response = await _http.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            
            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            
            _logger.LogError("Failed to resolve report: {Status} - {Error}", response.StatusCode, content);
            return false;
        }
    }
}
