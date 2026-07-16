using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TripMate_WebAPI.Services
{
    public class GuideApprovalService
    {
        private readonly HttpClient _http;
        private readonly string _supabaseUrl;
        private readonly string _anonKey;
        private readonly ILogger<GuideApprovalService> _logger;
        private readonly JsonSerializerOptions _json;
        private readonly AdminService _adminService;

        public GuideApprovalService(
            HttpClient http,
            IConfiguration config,
            ILogger<GuideApprovalService> logger,
            AdminService adminService)
        {
            _http = http;
            _supabaseUrl = config["Supabase:Url"] ?? throw new Exception("Supabase URL not configured");
            _anonKey = config["Supabase:AnonKey"] ?? throw new Exception("Supabase Anon Key not configured");
            _logger = logger;
            _json = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            _adminService = adminService;
        }

        // Get pending guide applications (is_verified = false)
        public async Task<List<GuideApplicationRow>> GetPendingApplicationsAsync()
        {
            try
            {
                var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"{_supabaseUrl}/rest/v1/guide_profiles?is_verified=eq.false&select=*,profiles(email,full_name,phone_number,role),guide_certificates(certificate_name,file_url)&order=created_at.asc");

                request.Headers.Add("apikey", _anonKey);
                request.Headers.Add("Prefer", "return=representation");

                var response = await _http.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to get pending applications: {Content}", content);
                    return new List<GuideApplicationRow>();
                }

                var rows = JsonSerializer.Deserialize<List<GuideProfileWithRelationsRow>>(content, _json) ?? new();
                var list = rows.Select(MapToDto).ToList();
                foreach (var item in list)
                {
                    if (!string.IsNullOrEmpty(item.Certificate_Path))
                    {
                        item.Certificate_Path = await _adminService.GetSignedUrlAsync(item.Certificate_Path);
                    }
                }
                return list;
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
                    $"{_supabaseUrl}/rest/v1/guide_profiles?is_verified=eq.false&select=id");

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

        // Get guide application by ID (guide_profiles.id)
        public async Task<GuideApplicationRow?> GetApplicationByIdAsync(string guideProfileId)
        {
            try
            {
                var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"{_supabaseUrl}/rest/v1/guide_profiles?id=eq.{guideProfileId}&select=*,profiles(email,full_name,phone_number,role),guide_certificates(certificate_name,file_url)");

                request.Headers.Add("apikey", _anonKey);

                var response = await _http.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to get application: {Content}", content);
                    return null;
                }

                var rows = JsonSerializer.Deserialize<List<GuideProfileWithRelationsRow>>(content, _json);
                var row = rows?.FirstOrDefault();
                if (row == null) return null;

                var dto = MapToDto(row);
                if (!string.IsNullOrEmpty(dto.Certificate_Path))
                {
                    dto.Certificate_Path = await _adminService.GetSignedUrlAsync(dto.Certificate_Path);
                }
                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting application by ID");
                return null;
            }
        }

        // Approve guide
        public async Task<bool> ApproveGuideAsync(string guideProfileId, string adminComment = "", string? accessToken = null)
        {
            try
            {
                var application = await GetApplicationByIdAsync(guideProfileId);
                if (application == null) return false;

                // 1. Update guide_profiles
                var updateProfileData = new
                {
                    is_verified = true,
                    verified_at = DateTime.UtcNow,
                    updated_at = DateTime.UtcNow
                };

                var req1 = new HttpRequestMessage(
                    HttpMethod.Patch,
                    $"{_supabaseUrl}/rest/v1/guide_profiles?id=eq.{guideProfileId}");
                
                req1.Headers.Add("apikey", _anonKey);
                if (!string.IsNullOrEmpty(accessToken)) req1.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                req1.Content = new StringContent(JsonSerializer.Serialize(updateProfileData), Encoding.UTF8, "application/json");

                var res1 = await _http.SendAsync(req1);
                if (!res1.IsSuccessStatusCode) return false;

                // 2. Update profiles role to 'guide'
                if (!string.IsNullOrEmpty(application.UserId))
                {
                    var req2 = new HttpRequestMessage(
                        HttpMethod.Patch,
                        $"{_supabaseUrl}/rest/v1/profiles?id=eq.{application.UserId}");
                    req2.Headers.Add("apikey", _anonKey);
                    if (!string.IsNullOrEmpty(accessToken)) req2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                    req2.Content = new StringContent(JsonSerializer.Serialize(new { role = "guide" }), Encoding.UTF8, "application/json");
                    await _http.SendAsync(req2);
                }

                _logger.LogInformation("Guide {GuideId} approved successfully", guideProfileId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving guide");
                return false;
            }
        }

        // Reject guide
        public async Task<bool> RejectGuideAsync(string guideProfileId, string reason, string? accessToken = null)
        {
            try
            {
                // To reject, we delete the guide_profiles row so they can apply again later.
                // (Alternatively, we could keep it and add a status column to the DB).
                var request = new HttpRequestMessage(
                    HttpMethod.Delete,
                    $"{_supabaseUrl}/rest/v1/guide_profiles?id=eq.{guideProfileId}");

                request.Headers.Add("apikey", _anonKey);
                if (!string.IsNullOrEmpty(accessToken))
                {
                    request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                }

                var response = await _http.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to reject guide: {Content}", content);
                    return false;
                }

                _logger.LogInformation("Guide {GuideId} rejected successfully", guideProfileId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting guide");
                return false;
            }
        }

        private static GuideApplicationRow MapToDto(GuideProfileWithRelationsRow row)
        {
            var firstCert = row.Certificates?.FirstOrDefault();

            return new GuideApplicationRow
            {
                Id = row.Id,
                UserId = row.UserId,
                Email = row.Profile?.Email,
                Full_Name = row.Profile?.FullName,
                Phone_Number = row.Profile?.PhoneNumber,
                Role = row.Profile?.Role,
                Bio = row.Bio,
                Languages = string.Join(", ", row.Languages ?? new List<string>()),
                Specialization = string.Join(", ", row.Specialties ?? new List<string>()),
                Status = row.IsVerified ? "active" : "pending",
                Created_At = row.CreatedAt,
                Updated_At = row.UpdatedAt,
                Approved_At = row.VerifiedAt,
                Certificate_Path = firstCert?.FileUrl
            };
        }
    }

    // DTO mapped to Admin view
    public class GuideApplicationRow
    {
        public string? Id { get; set; }
        public string? UserId { get; set; }
        public string? Email { get; set; }
        public string? Full_Name { get; set; }
        public string? Phone_Number { get; set; }
        public string? Role { get; set; }
        public string? Experience { get; set; } // Left blank intentionally, no matching column
        public string? Specialization { get; set; }
        public string? Languages { get; set; }
        public string? Bio { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("certificate_url")]
        public string? Certificate_Path { get; set; }
        
        public string? Status { get; set; }
        public string? Admin_Comment { get; set; }
        public DateTime? Created_At { get; set; }
        public DateTime? Updated_At { get; set; }
        public DateTime? Approved_At { get; set; }
        public DateTime? Rejected_At { get; set; }
    }

    // Internal Row Models matching database_setup.sql
    internal class GuideProfileWithRelationsRow
    {
        [JsonPropertyName("id")]              public string? Id { get; set; }
        [JsonPropertyName("user_id")]         public string? UserId { get; set; }
        [JsonPropertyName("bio")]             public string? Bio { get; set; }
        [JsonPropertyName("languages")]       public List<string>? Languages { get; set; }
        [JsonPropertyName("specialties")]     public List<string>? Specialties { get; set; }
        [JsonPropertyName("is_verified")]     public bool IsVerified { get; set; }
        [JsonPropertyName("verified_at")]     public DateTime? VerifiedAt { get; set; }
        [JsonPropertyName("created_at")]      public DateTime? CreatedAt { get; set; }
        [JsonPropertyName("updated_at")]      public DateTime? UpdatedAt { get; set; }

        [JsonPropertyName("profiles")]        public ProfileData? Profile { get; set; }
        [JsonPropertyName("guide_certificates")] public List<CertificateData>? Certificates { get; set; }
    }

    public class ProfileData
    {
        [JsonPropertyName("email")]        public string? Email { get; set; }
        [JsonPropertyName("full_name")]    public string? FullName { get; set; }
        [JsonPropertyName("phone_number")] public string? PhoneNumber { get; set; }
        [JsonPropertyName("role")]         public string? Role { get; set; }
        [JsonPropertyName("is_active")]    public bool IsActive { get; set; }
    }

    public class CertificateData
    {
        [JsonPropertyName("certificate_name")] public string? CertificateName { get; set; }
        [JsonPropertyName("file_url")]         public string? FileUrl { get; set; }
    }
}
