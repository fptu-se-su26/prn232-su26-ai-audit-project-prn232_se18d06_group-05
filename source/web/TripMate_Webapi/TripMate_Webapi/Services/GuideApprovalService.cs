using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace TripMate_WebAPI.Services
{
    public class GuideApprovalService
    {
        private readonly HttpClient _http;
        private readonly string _supabaseUrl;
        private readonly string _anonKey;
        private readonly ILogger<GuideApprovalService> _logger;
        private readonly JsonSerializerOptions _json;

        public GuideApprovalService(
            HttpClient http,
            IConfiguration config,
            ILogger<GuideApprovalService> logger)
        {
            _http = http;
            _supabaseUrl = config["Supabase:Url"] ?? throw new Exception("Supabase URL not configured");
            _anonKey = config["Supabase:AnonKey"] ?? throw new Exception("Supabase Anon Key not configured");
            _logger = logger;
            _json = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        // Get pending guide applications
        public async Task<List<GuideApplicationRow>> GetPendingApplicationsAsync()
        {
            try
            {
                var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"{_supabaseUrl}/rest/v1/profiles?role=eq.guide&status=eq.pending&select=*&order=created_at.desc");

                request.Headers.Add("apikey", _anonKey);
                request.Headers.Add("Prefer", "return=representation");

                var response = await _http.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to get pending applications: {Content}", content);
                    return new List<GuideApplicationRow>();
                }

                var applications = JsonSerializer.Deserialize<List<GuideApplicationRow>>(content, _json);
                return applications ?? new List<GuideApplicationRow>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending applications");
                return new List<GuideApplicationRow>();
            }
        }

        // Get pending count
        public async Task<int> GetPendingCountAsync()
        {
            try
            {
                var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"{_supabaseUrl}/rest/v1/profiles?role=eq.guide&status=eq.pending&select=id");

                request.Headers.Add("apikey", _anonKey);
                request.Headers.Add("Prefer", "count=exact");

                var response = await _http.SendAsync(request);
                
                if (response.Headers.TryGetValues("Content-Range", out var values))
                {
                    var range = values.FirstOrDefault();
                    if (range != null && range.Contains("/"))
                    {
                        var parts = range.Split('/');
                        if (int.TryParse(parts[1], out int count))
                        {
                            return count;
                        }
                    }
                }

                return 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending count");
                return 0;
            }
        }

        // Get guide application by ID
        public async Task<GuideApplicationRow?> GetApplicationByIdAsync(string guideId)
        {
            try
            {
                var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"{_supabaseUrl}/rest/v1/profiles?id=eq.{guideId}&select=*");

                request.Headers.Add("apikey", _anonKey);

                var response = await _http.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to get application: {Content}", content);
                    return null;
                }

                var applications = JsonSerializer.Deserialize<List<GuideApplicationRow>>(content, _json);
                return applications?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting application by ID");
                return null;
            }
        }

        // Approve guide
        public async Task<bool> ApproveGuideAsync(string guideId, string adminComment = "")
        {
            try
            {
                var updateData = new
                {
                    status = "active",
                    admin_comment = adminComment,
                    approved_at = DateTime.UtcNow,
                    updated_at = DateTime.UtcNow
                };

                var request = new HttpRequestMessage(
                    HttpMethod.Patch,
                    $"{_supabaseUrl}/rest/v1/profiles?id=eq.{guideId}");

                request.Headers.Add("apikey", _anonKey);
                request.Headers.Add("Prefer", "return=minimal");
                request.Content = new StringContent(
                    JsonSerializer.Serialize(updateData),
                    Encoding.UTF8,
                    "application/json");

                var response = await _http.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to approve guide: {Content}", content);
                    return false;
                }

                _logger.LogInformation("Guide {GuideId} approved successfully", guideId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving guide");
                return false;
            }
        }

        // Reject guide
        public async Task<bool> RejectGuideAsync(string guideId, string reason)
        {
            try
            {
                var updateData = new
                {
                    status = "rejected",
                    admin_comment = reason,
                    rejected_at = DateTime.UtcNow,
                    updated_at = DateTime.UtcNow
                };

                var request = new HttpRequestMessage(
                    HttpMethod.Patch,
                    $"{_supabaseUrl}/rest/v1/profiles?id=eq.{guideId}");

                request.Headers.Add("apikey", _anonKey);
                request.Headers.Add("Prefer", "return=minimal");
                request.Content = new StringContent(
                    JsonSerializer.Serialize(updateData),
                    Encoding.UTF8,
                    "application/json");

                var response = await _http.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to reject guide: {Content}", content);
                    return false;
                }

                _logger.LogInformation("Guide {GuideId} rejected successfully", guideId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting guide");
                return false;
            }
        }
    }

    // DTOs
    public class GuideApplicationRow
    {
        public string? Id { get; set; }
        public string? Email { get; set; }
        public string? Full_Name { get; set; }
        public string? Phone_Number { get; set; }
        public string? Role { get; set; }
        public string? Experience { get; set; }
        public string? Specialization { get; set; }
        public string? Languages { get; set; }
        public string? Bio { get; set; }
        public string? Certificate_Path { get; set; }
        public string? Status { get; set; }
        public string? Admin_Comment { get; set; }
        public DateTime? Created_At { get; set; }
        public DateTime? Updated_At { get; set; }
        public DateTime? Approved_At { get; set; }
        public DateTime? Rejected_At { get; set; }
    }
}
