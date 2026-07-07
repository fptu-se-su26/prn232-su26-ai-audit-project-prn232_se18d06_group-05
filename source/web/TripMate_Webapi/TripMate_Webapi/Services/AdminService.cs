using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TripMate_WebAPI.DTOs.Auth;
using TripMate_WebAPI.Services;

namespace TripMate_WebAPI.Services
{
    public class KpisDto
    {
        public decimal TotalGmv { get; set; }
        public decimal PlatformRevenue { get; set; }
        public decimal EscrowHeld { get; set; }
        public decimal PendingDisbursement { get; set; }
    }

    public class AdminService
    {
        private readonly HttpClient _http;
        private readonly string _supabaseUrl;
        private readonly string _anonKey;
        private readonly string _serviceRoleKey;
        private readonly ILogger<AdminService> _logger;
        private readonly INotificationService _notif;

        private static readonly JsonSerializerOptions _json = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        public AdminService(
            HttpClient http,
            IConfiguration config,
            ILogger<AdminService> logger,
            INotificationService notif)
        {
            _http = http;
            _supabaseUrl = config["Supabase:Url"] ?? throw new Exception("Supabase URL not configured");
            _anonKey = config["Supabase:AnonKey"] ?? throw new Exception("Supabase Anon Key not configured");
            _serviceRoleKey = config["Supabase:ServiceRoleKey"] ?? throw new Exception("Supabase Service Role Key not configured");
            _logger = logger;
            _notif = notif;
        }

        // Helper to build request with Service Role Key (bypasses RLS)
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

        // Get pre-signed URL for private bucket
        public async Task<string> GetSignedUrlAsync(string url)
        {
            if (string.IsNullOrEmpty(url)) return url;
            if (!url.Contains("/storage/v1/object/private/")) return url;

            try
            {
                var idx = url.IndexOf("/storage/v1/object/private/");
                var subPath = url.Substring(idx + "/storage/v1/object/private/".Length);
                var slashIdx = subPath.IndexOf('/');
                if (slashIdx == -1) return url;

                var bucket = subPath.Substring(0, slashIdx);
                var filePath = subPath.Substring(slashIdx + 1);

                var reqUrl = $"{_supabaseUrl}/storage/v1/object/sign/{bucket}/{Uri.EscapeDataString(filePath)}";
                var request = BuildAdminRequest(HttpMethod.Post, reqUrl);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(new { expiresIn = 3600 }), 
                    Encoding.UTF8, 
                    "application/json");

                var response = await _http.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();
                
                if (response.IsSuccessStatusCode)
                {
                    using var doc = JsonDocument.Parse(content);
                    if (doc.RootElement.TryGetProperty("signedURL", out var prop) || 
                        doc.RootElement.TryGetProperty("signedUrl", out prop))
                    {
                        var signedPath = prop.GetString();
                        if (!string.IsNullOrEmpty(signedPath))
                        {
                            if (signedPath.StartsWith("/"))
                            {
                                return _supabaseUrl + signedPath;
                            }
                            return signedPath;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error signing Supabase private URL: {Url}", url);
            }
            return url;
        }

        // Fetch KPI Metrics from bookings
        public async Task<KpisDto> GetKpisAsync()
        {
            try
            {
                var url = $"{_supabaseUrl}/rest/v1/bookings?select=status,total_amount,platform_fee,guide_earnings,escrow_released";
                var request = BuildAdminRequest(HttpMethod.Get, url);
                var response = await _http.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();
                EnsureSuccess(response, content);

                var bookings = JsonSerializer.Deserialize<List<BookingKpiRow>>(content, _json) ?? new();

                decimal totalGmv = 0;
                decimal platformRevenue = 0;
                decimal escrowHeld = 0;
                decimal pendingDisbursement = 0;

                foreach (var b in bookings)
                {
                    // Statuses: 0=Pending, 1=Confirmed, 2=Completed, 3=Cancelled
                    if (b.Status != 3) // Exclude cancelled
                    {
                        totalGmv += b.TotalAmount;
                    }

                    if (b.Status == 2) // Completed
                    {
                        platformRevenue += b.PlatformFee;
                    }

                    if (b.Status == 1 && !b.EscrowReleased) // Confirmed & Escrow held
                    {
                        escrowHeld += b.TotalAmount;
                    }

                    if (b.Status == 2 && !b.EscrowReleased) // Completed but not yet released (pending disbursement to guide)
                    {
                        pendingDisbursement += b.GuideEarnings;
                    }
                }

                return new KpisDto
                {
                    TotalGmv = totalGmv,
                    PlatformRevenue = platformRevenue,
                    EscrowHeld = escrowHeld,
                    PendingDisbursement = pendingDisbursement
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error computing KPIs");
                return new KpisDto();
            }
        }

        // Bulk Release Escrow
        public async Task<bool> ReleaseEscrowBulkAsync(List<string> bookingIds)
        {
            try
            {
                foreach (var id in bookingIds)
                {
                    // 1. Get booking info
                    var url = $"{_supabaseUrl}/rest/v1/bookings?id=eq.{id}&select=*";
                    var request = BuildAdminRequest(HttpMethod.Get, url);
                    var response = await _http.SendAsync(request);
                    var content = await response.Content.ReadAsStringAsync();
                    EnsureSuccess(response, content);

                    var rows = JsonSerializer.Deserialize<List<BookingKpiRow>>(content, _json);
                    var b = rows?.FirstOrDefault();
                    if (b == null || b.EscrowReleased) continue;

                    // 2. Update booking: escrow_released = true, status = 2 (Completed) if it was Confirmed
                    var newStatus = b.Status == 1 ? 2 : b.Status;
                    var updates = new { escrow_released = true, status = newStatus, updated_at = DateTime.UtcNow };
                    var patchReq = BuildAdminRequest(HttpMethod.Patch, $"{_supabaseUrl}/rest/v1/bookings?id=eq.{id}");
                    patchReq.Content = new StringContent(JsonSerializer.Serialize(updates), Encoding.UTF8, "application/json");
                    var patchRes = await _http.SendAsync(patchReq);
                    EnsureSuccess(patchRes, await patchRes.Content.ReadAsStringAsync());

                    // 3. Record entries in ledger_entries table
                    // A) platform fee entry (FEE)
                    var feeLedger = new
                    {
                        booking_id = id,
                        user_id = b.TravelerId, // System records traveler paying fee
                        type = "FEE",
                        amount = b.PlatformFee,
                        created_at = DateTime.UtcNow
                    };
                    var feeReq = BuildAdminRequest(HttpMethod.Post, $"{_supabaseUrl}/rest/v1/ledger_entries");
                    feeReq.Content = new StringContent(JsonSerializer.Serialize(feeLedger), Encoding.UTF8, "application/json");
                    await _http.SendAsync(feeReq);

                    // B) guide earnings entry (EARNING)
                    // Get guide user_id
                    var guideUserUrl = $"{_supabaseUrl}/rest/v1/guide_profiles?id=eq.{b.GuideProfileId}&select=user_id";
                    var guideUserReq = BuildAdminRequest(HttpMethod.Get, guideUserUrl);
                    var guideUserRes = await _http.SendAsync(guideUserReq);
                    var guideUserContent = await guideUserRes.Content.ReadAsStringAsync();
                    using var guideDoc = JsonDocument.Parse(guideUserContent);
                    var guideUserId = guideDoc.RootElement.EnumerateArray().FirstOrDefault().GetProperty("user_id").GetString();

                    if (!string.IsNullOrEmpty(guideUserId))
                    {
                        var earningLedger = new
                        {
                            booking_id = id,
                            user_id = guideUserId,
                            type = "EARNING",
                            amount = b.GuideEarnings,
                            created_at = DateTime.UtcNow
                        };
                        var earnReq = BuildAdminRequest(HttpMethod.Post, $"{_supabaseUrl}/rest/v1/ledger_entries");
                        earnReq.Content = new StringContent(JsonSerializer.Serialize(earningLedger), Encoding.UTF8, "application/json");
                        await _http.SendAsync(earnReq);

                        // Trigger notifications
                        await _notif.SendAsync(guideUserId, "escrow_released", "Your escrow funds have been released!", $"The amount of {b.GuideEarnings:N0}₫ has been paid.", new { bookingId = id });
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error releasing escrow bulk");
                return false;
            }
        }

        // Adjust commission rate & override platform fee
        public async Task<bool> OverridePlatformFeeAsync(string bookingId, decimal platformFee)
        {
            try
            {
                // 1. Get booking
                var url = $"{_supabaseUrl}/rest/v1/bookings?id=eq.{bookingId}&select=*";
                var request = BuildAdminRequest(HttpMethod.Get, url);
                var response = await _http.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();
                EnsureSuccess(response, content);

                var rows = JsonSerializer.Deserialize<List<BookingKpiRow>>(content, _json);
                var b = rows?.FirstOrDefault();
                if (b == null) return false;

                // Recalculate guide earnings
                var guideEarnings = b.TotalAmount - platformFee;

                var updates = new { platform_fee = platformFee, guide_earnings = guideEarnings, updated_at = DateTime.UtcNow };
                var patchReq = BuildAdminRequest(HttpMethod.Patch, $"{_supabaseUrl}/rest/v1/bookings?id=eq.{bookingId}");
                patchReq.Content = new StringContent(JsonSerializer.Serialize(updates), Encoding.UTF8, "application/json");
                
                var patchRes = await _http.SendAsync(patchReq);
                EnsureSuccess(patchRes, await patchRes.Content.ReadAsStringAsync());

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error overriding platform fee");
                return false;
            }
        }

        // Approve Booking Cancel (State 3 - Cancelled)
        public async Task<bool> ApproveCancelAsync(string bookingId, bool approve)
        {
            try
            {
                var status = approve ? 3 : 1; // 3 = Cancelled, 1 = Restored back to Confirmed
                var updates = new { status = status, updated_at = DateTime.UtcNow };
                
                var patchReq = BuildAdminRequest(HttpMethod.Patch, $"{_supabaseUrl}/rest/v1/bookings?id=eq.{bookingId}");
                patchReq.Content = new StringContent(JsonSerializer.Serialize(updates), Encoding.UTF8, "application/json");
                
                var patchRes = await _http.SendAsync(patchReq);
                EnsureSuccess(patchRes, await patchRes.Content.ReadAsStringAsync());

                // Trigger refund ledger entries if approved and escrow was already released or held
                if (approve)
                {
                    // A refund ledger record can be logged
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving cancellation request");
                return false;
            }
        }

        // Direct SQL/Supabase role update
        public async Task<bool> UpdateUserRoleAsync(string userId, string newRole)
        {
            try
            {
                var updates = new { role = newRole, updated_at = DateTime.UtcNow };
                var patchReq = BuildAdminRequest(HttpMethod.Patch, $"{_supabaseUrl}/rest/v1/profiles?id=eq.{userId}");
                patchReq.Content = new StringContent(JsonSerializer.Serialize(updates), Encoding.UTF8, "application/json");
                
                var patchRes = await _http.SendAsync(patchReq);
                EnsureSuccess(patchRes, await patchRes.Content.ReadAsStringAsync());

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user role");
                return false;
            }
        }

        // Toggle user active status
        public async Task<bool> ToggleUserActiveAsync(string userId, bool isActive)
        {
            try
            {
                var updates = new { is_active = isActive, updated_at = DateTime.UtcNow };
                var patchReq = BuildAdminRequest(HttpMethod.Patch, $"{_supabaseUrl}/rest/v1/profiles?id=eq.{userId}");
                patchReq.Content = new StringContent(JsonSerializer.Serialize(updates), Encoding.UTF8, "application/json");
                
                var patchRes = await _http.SendAsync(patchReq);
                EnsureSuccess(patchRes, await patchRes.Content.ReadAsStringAsync());

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling user status");
                return false;
            }
        }

        // Get reviews list
        public async Task<List<AdminReviewRow>> GetReviewsAsync()
        {
            try
            {
                var url = $"{_supabaseUrl}/rest/v1/reviews?select=*,profiles:traveler_id(full_name,email)";
                var request = BuildAdminRequest(HttpMethod.Get, url);
                var response = await _http.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();
                EnsureSuccess(response, content);

                return JsonSerializer.Deserialize<List<AdminReviewRow>>(content, _json) ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading reviews");
                return new List<AdminReviewRow>();
            }
        }

        // Moderate/Hide review
        public async Task<bool> ModerateReviewAsync(string reviewId, string adminNote)
        {
            try
            {
                // Update review comment with moderated text and append note
                var updates = new { comment = $"[Review hidden by Admin. Reason: {adminNote}]" };
                var patchReq = BuildAdminRequest(HttpMethod.Patch, $"{_supabaseUrl}/rest/v1/reviews?id=eq.{reviewId}");
                patchReq.Content = new StringContent(JsonSerializer.Serialize(updates), Encoding.UTF8, "application/json");
                
                var patchRes = await _http.SendAsync(patchReq);
                EnsureSuccess(patchRes, await patchRes.Content.ReadAsStringAsync());

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moderating review");
                return false;
            }
        }

        // Get ledger entries
        public async Task<List<LedgerRow>> GetLedgerEntriesAsync()
        {
            try
            {
                var url = $"{_supabaseUrl}/rest/v1/ledger_entries?select=*,bookings(id,total_amount,platform_fee,guide_earnings,experience_packages(title))";
                var request = BuildAdminRequest(HttpMethod.Get, url);
                var response = await _http.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();
                EnsureSuccess(response, content);

                return JsonSerializer.Deserialize<List<LedgerRow>>(content, _json) ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting ledger entries");
                return new List<LedgerRow>();
            }
        }

        // Get all bookings with joins for the admin panel
        public async Task<List<AdminBookingRow>> GetBookingsAsync()
        {
            try
            {
                var url = $"{_supabaseUrl}/rest/v1/bookings?select=*,profiles:traveler_id(full_name,email),experience_packages(title,guide_profiles(profiles(full_name)))&order=created_at.desc";
                var request = BuildAdminRequest(HttpMethod.Get, url);
                var response = await _http.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();
                EnsureSuccess(response, content);

                return JsonSerializer.Deserialize<List<AdminBookingRow>>(content, _json) ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all bookings for admin");
                return new List<AdminBookingRow>();
            }
        }

        // Get chat logs for a booking
        public async Task<List<AdminChatMessageRow>> GetChatMessagesAsync(string bookingId)
        {
            try
            {
                var url = $"{_supabaseUrl}/rest/v1/chat_messages?booking_id=eq.{bookingId}&select=*,sender:sender_id(full_name,email),receiver:receiver_id(full_name,email)&order=sent_at.asc";
                var request = BuildAdminRequest(HttpMethod.Get, url);
                var response = await _http.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();
                EnsureSuccess(response, content);

                return JsonSerializer.Deserialize<List<AdminChatMessageRow>>(content, _json) ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting chat messages for booking {BookingId}", bookingId);
                return new List<AdminChatMessageRow>();
            }
        }

        // Get all users in the system
        public async Task<List<ProfileRow>> GetUsersAsync()
        {
            try
            {
                var url = $"{_supabaseUrl}/rest/v1/profiles?select=*&order=created_at.desc";
                var request = BuildAdminRequest(HttpMethod.Get, url);
                var response = await _http.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();
                EnsureSuccess(response, content);

                return JsonSerializer.Deserialize<List<ProfileRow>>(content, _json) ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all users");
                return new List<ProfileRow>();
            }
        }

        // Get all guides with profile information
        public async Task<List<AdminGuideProfileRow>> GetGuidesAsync()
        {
            try
            {
                var url = $"{_supabaseUrl}/rest/v1/guide_profiles?select=*,profiles:user_id(full_name,email,is_active)&order=created_at.desc";
                var request = BuildAdminRequest(HttpMethod.Get, url);
                var response = await _http.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();
                EnsureSuccess(response, content);

                return JsonSerializer.Deserialize<List<AdminGuideProfileRow>>(content, _json) ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all guide profiles");
                return new List<AdminGuideProfileRow>();
            }
        }

        // Update guide account status (is_active in profiles table)
        public async Task<bool> UpdateGuideStatusAsync(string guideId, string status)
        {
            try
            {
                // 1. Get user_id from guide_profiles
                var getUrl = $"{_supabaseUrl}/rest/v1/guide_profiles?id=eq.{guideId}&select=user_id";
                var getReq = BuildAdminRequest(HttpMethod.Get, getUrl);
                var getRes = await _http.SendAsync(getReq);
                var getContent = await getRes.Content.ReadAsStringAsync();
                EnsureSuccess(getRes, getContent);

                var guideList = JsonSerializer.Deserialize<List<AdminGuideProfileRow>>(getContent, _json);
                if (guideList == null || !guideList.Any() || string.IsNullOrEmpty(guideList[0].UserId))
                {
                    return false;
                }
                var userId = guideList[0].UserId;

                // 2. Patch is_active in profiles
                bool isActive = status.ToLower() == "active";
                var updates = new { is_active = isActive, updated_at = DateTime.UtcNow };

                var patchReq = BuildAdminRequest(HttpMethod.Patch, $"{_supabaseUrl}/rest/v1/profiles?id=eq.{userId}");
                patchReq.Content = new StringContent(JsonSerializer.Serialize(updates), Encoding.UTF8, "application/json");

                var patchRes = await _http.SendAsync(patchReq);
                EnsureSuccess(patchRes, await patchRes.Content.ReadAsStringAsync());

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating guide status for GuideId {GuideId}", guideId);
                return false;
            }
        }

        // Toggle guide verification state
        public async Task<bool> ToggleGuideVerificationAsync(string guideId, bool isVerified)
        {
            try
            {
                var updates = new 
                { 
                    is_verified = isVerified, 
                    verified_at = isVerified ? (DateTime?)DateTime.UtcNow : null, 
                    updated_at = DateTime.UtcNow 
                };
                var patchReq = BuildAdminRequest(HttpMethod.Patch, $"{_supabaseUrl}/rest/v1/guide_profiles?id=eq.{guideId}");
                patchReq.Content = new StringContent(JsonSerializer.Serialize(updates), Encoding.UTF8, "application/json");
                
                var patchRes = await _http.SendAsync(patchReq);
                EnsureSuccess(patchRes, await patchRes.Content.ReadAsStringAsync());

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error toggling guide verification for {GuideId}", guideId);
                return false;
            }
        }

        public async Task<bool> DeleteUserAsync(string userId)
        {
            try
            {
                var url = $"{_supabaseUrl}/auth/v1/admin/users/{userId}";
                var req = BuildAdminRequest(HttpMethod.Delete, url);
                
                var response = await _http.SendAsync(req);
                var content = await response.Content.ReadAsStringAsync();
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("User {UserId} deleted successfully from Supabase Auth", userId);
                    return true;
                }
                else
                {
                    _logger.LogError("Failed to delete user {UserId}: {StatusCode} - {Content}", userId, response.StatusCode, content);
                    
                    // Fallback: if auth deletion fails or is restricted, try deleting from profiles directly
                    var profileUrl = $"{_supabaseUrl}/rest/v1/profiles?id=eq.{userId}";
                    var profileReq = BuildAdminRequest(HttpMethod.Delete, profileUrl);
                    var profileResponse = await _http.SendAsync(profileReq);
                    if (profileResponse.IsSuccessStatusCode)
                    {
                        _logger.LogInformation("Deleted profile row for user {UserId} from public.profiles directly", userId);
                        return true;
                    }
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", userId);
                return false;
            }
        }
    }

    public class AdminGuideProfileRow
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("user_id")] public string? UserId { get; set; }
        [JsonPropertyName("bio")] public string? Bio { get; set; }
        [JsonPropertyName("city_area")] public string? CityArea { get; set; }
        [JsonPropertyName("experience")] public string? Experience { get; set; }
        [JsonPropertyName("specialties")] public List<string>? Specialties { get; set; }
        [JsonPropertyName("languages")] public List<string>? Languages { get; set; }
        [JsonPropertyName("certificate_url")] public string? CertificateUrl { get; set; }
        [JsonPropertyName("cover_photo_url")] public string? CoverPhotoUrl { get; set; }
        [JsonPropertyName("status")] public string? Status { get; set; }
        [JsonPropertyName("average_rating")] public decimal AverageRating { get; set; }
        [JsonPropertyName("total_reviews")] public int TotalReviews { get; set; }
        [JsonPropertyName("is_verified")] public bool IsVerified { get; set; }
        [JsonPropertyName("verified_at")] public DateTime? VerifiedAt { get; set; }
        [JsonPropertyName("profiles")] public ProfileData? Profile { get; set; }
    }

    public class AdminChatMessageRow
    {
        [JsonPropertyName("id")] public long Id { get; set; }
        [JsonPropertyName("booking_id")] public string? BookingId { get; set; }
        [JsonPropertyName("sender_id")] public string? SenderId { get; set; }
        [JsonPropertyName("receiver_id")] public string? ReceiverId { get; set; }
        [JsonPropertyName("message_text")] public string? MessageText { get; set; }
        [JsonPropertyName("is_read")] public bool IsRead { get; set; }
        [JsonPropertyName("sent_at")] public DateTime SentAt { get; set; }
        [JsonPropertyName("sender")] public ProfileData? Sender { get; set; }
        [JsonPropertyName("receiver")] public ProfileData? Receiver { get; set; }
    }

    internal class BookingKpiRow
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("traveler_id")] public string? TravelerId { get; set; }
        [JsonPropertyName("guide_profile_id")] public string? GuideProfileId { get; set; }
        [JsonPropertyName("status")] public int Status { get; set; }
        [JsonPropertyName("total_amount")] public decimal TotalAmount { get; set; }
        [JsonPropertyName("platform_fee")] public decimal PlatformFee { get; set; }
        [JsonPropertyName("guide_earnings")] public decimal GuideEarnings { get; set; }
        [JsonPropertyName("escrow_released")] public bool EscrowReleased { get; set; }
    }

    public class AdminReviewRow
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("booking_id")] public string? BookingId { get; set; }
        [JsonPropertyName("rating")] public int Rating { get; set; }
        [JsonPropertyName("comment")] public string? Comment { get; set; }
        [JsonPropertyName("created_at")] public DateTime CreatedAt { get; set; }
        [JsonPropertyName("profiles")] public ProfileData? Profile { get; set; }
    }

    public class LedgerRow
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("booking_id")] public string? BookingId { get; set; }
        [JsonPropertyName("user_id")] public string? UserId { get; set; }
        [JsonPropertyName("type")] public string? Type { get; set; }
        [JsonPropertyName("amount")] public decimal Amount { get; set; }
        [JsonPropertyName("created_at")] public DateTime CreatedAt { get; set; }
        [JsonPropertyName("bookings")] public LedgerBookingJoined? Booking { get; set; }
    }

    public class LedgerBookingJoined
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("total_amount")] public decimal TotalAmount { get; set; }
        [JsonPropertyName("platform_fee")] public decimal PlatformFee { get; set; }
        [JsonPropertyName("guide_earnings")] public decimal GuideEarnings { get; set; }
        [JsonPropertyName("experience_packages")] public LedgerPackageJoined? Package { get; set; }
    }

    public class LedgerPackageJoined
    {
        [JsonPropertyName("title")] public string? Title { get; set; }
    }

    public class AdminBookingRow
    {
        [JsonPropertyName("id")] public string? Id { get; set; }
        [JsonPropertyName("traveler_id")] public string? TravelerId { get; set; }
        [JsonPropertyName("guide_profile_id")] public string? GuideProfileId { get; set; }
        [JsonPropertyName("experience_package_id")] public string? ExperiencePackageId { get; set; }
        [JsonPropertyName("booking_date")] public string? BookingDate { get; set; }
        [JsonPropertyName("start_time")] public string? StartTime { get; set; }
        [JsonPropertyName("guest_count")] public int GuestCount { get; set; }
        [JsonPropertyName("total_amount")] public decimal TotalAmount { get; set; }
        [JsonPropertyName("platform_fee")] public decimal PlatformFee { get; set; }
        [JsonPropertyName("guide_earnings")] public decimal GuideEarnings { get; set; }
        [JsonPropertyName("status")] public int Status { get; set; }
        [JsonPropertyName("escrow_released")] public bool EscrowReleased { get; set; }
        [JsonPropertyName("cancel_reason")] public string? CancelReason { get; set; }
        [JsonPropertyName("created_at")] public DateTime CreatedAt { get; set; }
        [JsonPropertyName("profiles")] public ProfileData? Traveler { get; set; }
        [JsonPropertyName("experience_packages")] public BookingPackageJoined? Package { get; set; }
    }

    public class BookingPackageJoined
    {
        [JsonPropertyName("title")] public string? Title { get; set; }
        [JsonPropertyName("guide_profiles")] public BookingGuideJoined? GuideProfile { get; set; }
    }

    public class BookingGuideJoined
    {
        [JsonPropertyName("profiles")] public ProfileData? Profile { get; set; }
    }
}
