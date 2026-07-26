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

                    if (b.Status == 1) // Confirmed & Escrow held
                    {
                        escrowHeld += b.TotalAmount;
                    }

                    if (b.Status == 2 && !b.EscrowReleased) // Completed and still awaiting disbursement
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
            string? activeBookingId = null;
            string? activeGuideUserId = null;
            try
            {
                if (bookingIds == null || bookingIds.Count == 0)
                {
                    _logger.LogWarning("Escrow release was rejected because no booking IDs were supplied");
                    return false;
                }

                var bookingsToRelease = new List<(string Id, BookingKpiRow Booking, string GuideUserId)>();

                // Validate the complete batch before writing any ledger entries. This prevents
                // an ineligible booking later in the request from causing a partial release.
                foreach (var id in bookingIds.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct())
                {
                    activeBookingId = id;
                    var url = $"{_supabaseUrl}/rest/v1/bookings?id=eq.{id}&select=*";
                    var request = BuildAdminRequest(HttpMethod.Get, url);
                    var response = await _http.SendAsync(request);
                    var content = await response.Content.ReadAsStringAsync();
                    EnsureSuccess(response, content);

                    var rows = JsonSerializer.Deserialize<List<BookingKpiRow>>(content, _json);
                    var b = rows?.FirstOrDefault();
                    if (b == null)
                    {
                        throw new InvalidOperationException($"Cannot release escrow for booking {id}: booking was not found.");
                    }

                    if (b.Status != 2)
                    {
                        throw new InvalidOperationException(
                            $"Cannot release escrow for booking {id}: booking status must be Completed (2), but was {b.Status}.");
                    }

                    if (!string.Equals(b.CompletionState, "confirmed", StringComparison.Ordinal))
                    {
                        throw new InvalidOperationException(
                            $"Cannot release escrow for booking {id}: both parties have not confirmed completion.");
                    }

                    if (b.AmountPaid < b.TotalAmount)
                    {
                        throw new InvalidOperationException(
                            $"Cannot release escrow for booking {id}: booking is not fully paid ({b.AmountPaid:N2}/{b.TotalAmount:N2}).");
                    }

                    if (b.EscrowReleased)
                    {
                        throw new InvalidOperationException($"Cannot release escrow for booking {id}: escrow has already been released.");
                    }

                    if (b.PayoutStatus is not ("eligible" or "failed"))
                    {
                        throw new InvalidOperationException(
                            $"Cannot release escrow for booking {id}: payout state is {b.PayoutStatus}.");
                    }

                    var guideUserId = await GetGuideUserIdAsync(b.GuideProfileId);
                    if (string.IsNullOrWhiteSpace(guideUserId))
                    {
                        throw new InvalidOperationException(
                            $"Cannot release escrow for booking {id}: the booking has no valid guide account.");
                    }

                    bookingsToRelease.Add((id, b, guideUserId));
                }

                if (bookingsToRelease.Count == 0)
                {
                    _logger.LogWarning("Escrow release was rejected because no valid booking IDs were supplied");
                    return false;
                }

                foreach (var (id, b, guideUserId) in bookingsToRelease)
                {
                    activeBookingId = id;
                    activeGuideUserId = guideUserId;

                    // The database function locks the booking, validates payment/completion,
                    // writes both ledger rows with deterministic idempotency keys, and marks
                    // the payout released in one transaction.
                    var releaseRequest = BuildAdminRequest(
                        HttpMethod.Post,
                        $"{_supabaseUrl}/rest/v1/rpc/release_booking_payout");
                    releaseRequest.Content = new StringContent(
                        JsonSerializer.Serialize(new { p_booking_id = id }),
                        Encoding.UTF8,
                        "application/json");
                    var releaseResponse = await _http.SendAsync(releaseRequest);
                    EnsureSuccess(
                        releaseResponse,
                        await releaseResponse.Content.ReadAsStringAsync());

                    if (!string.IsNullOrEmpty(guideUserId))
                    {
                        await _notif.SendAsync(
                            guideUserId,
                            NotificationTypes.PayoutReleased,
                            "Earnings credited",
                            $"{b.GuideEarnings:N0}₫ from booking {id} has been credited to your TripMate earnings.",
                            new { bookingId = id, amount = b.GuideEarnings },
                            "/Guide/Earnings",
                            $"payout-released:{id}",
                            sendEmail: true);

                        await _notif.SendAsync(
                            guideUserId,
                            NotificationTypes.BookingCompleted,
                            "Booking completed",
                            $"Booking {id} is now complete.",
                            new { bookingId = id },
                            "/Guide/Bookings",
                            $"booking-completed:{id}:guide");
                    }

                    if (!string.IsNullOrWhiteSpace(b.TravelerId))
                    {
                        await _notif.SendAsync(
                            b.TravelerId,
                            NotificationTypes.BookingCompleted,
                            "Trip completed",
                            "Your trip is complete. We hope you had a wonderful experience.",
                            new { bookingId = id },
                            $"/Traveler/BookingDetails/{id}",
                            $"booking-completed:{id}:traveler");
                        await _notif.SendAsync(
                            b.TravelerId,
                            NotificationTypes.ReviewRequested,
                            "How was your trip?",
                            "Share a review to help your guide and other travelers.",
                            new { bookingId = id },
                            $"/Traveler/Review/{id}",
                            $"review-request:{id}");
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error releasing escrow bulk");
                if (!string.IsNullOrWhiteSpace(activeGuideUserId))
                {
                    await _notif.SendAsync(
                        activeGuideUserId,
                        NotificationTypes.PayoutFailed,
                        "Payout needs attention",
                        $"We could not release the payout for booking {activeBookingId}. Support has been notified.",
                        new { bookingId = activeBookingId },
                        "/Guide/Support",
                        $"payout-failed:{activeBookingId}",
                        sendEmail: true);
                }
                await _notif.SendToRoleAsync(
                    "admin",
                    NotificationTypes.PayoutFailed,
                    "Payout release failed",
                    $"Escrow release failed for booking {activeBookingId}.",
                    new { bookingId = activeBookingId },
                    "/Admin/Escrow",
                    $"payout-failed:{activeBookingId}:admin");
                throw;
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
                var bookingRequest = BuildAdminRequest(HttpMethod.Get, $"{_supabaseUrl}/rest/v1/bookings?id=eq.{bookingId}&select=*");
                var bookingResponse = await _http.SendAsync(bookingRequest);
                var bookingContent = await bookingResponse.Content.ReadAsStringAsync();
                EnsureSuccess(bookingResponse, bookingContent);
                var booking = JsonSerializer.Deserialize<List<BookingKpiRow>>(bookingContent, _json)?.FirstOrDefault();
                if (booking is null) return false;

                var status = approve ? 3 : 1; // 3 = Cancelled, 1 = Restored back to Confirmed
                var updates = approve 
                    ? (object)new { status = status, completion_state = "cancelled", updated_at = DateTime.UtcNow }
                    : (object)new { status = status, cancel_reason = (string?)null, updated_at = DateTime.UtcNow };
                
                var patchReq = BuildAdminRequest(HttpMethod.Patch, $"{_supabaseUrl}/rest/v1/bookings?id=eq.{bookingId}");
                patchReq.Content = new StringContent(JsonSerializer.Serialize(updates), Encoding.UTF8, "application/json");
                
                var patchRes = await _http.SendAsync(patchReq);
                EnsureSuccess(patchRes, await patchRes.Content.ReadAsStringAsync());

                if (approve)
                {
                    // 1. Record refund in ledger entries
                    if (booking.AmountPaid > 0 && !string.IsNullOrWhiteSpace(booking.TravelerId))
                    {
                        var refundLedger = new
                        {
                            booking_id = bookingId,
                            user_id = booking.TravelerId,
                            type = "REFUND",
                            amount = booking.AmountPaid,
                            created_at = DateTime.UtcNow
                        };
                        var refundReq = BuildAdminRequest(HttpMethod.Post, $"{_supabaseUrl}/rest/v1/ledger_entries");
                        refundReq.Content = new StringContent(JsonSerializer.Serialize(refundLedger), Encoding.UTF8, "application/json");
                        var refundRes = await _http.SendAsync(refundReq);
                        EnsureSuccess(refundRes, await refundRes.Content.ReadAsStringAsync());
                    }

                    // 2. Penalty logic for GuideNoShow
                    if (booking.CancelReason == "GuideNoShow" && !string.IsNullOrWhiteSpace(booking.GuideProfileId))
                    {
                        // Get guide profile
                        var gpUrl = $"{_supabaseUrl}/rest/v1/guide_profiles?id=eq.{booking.GuideProfileId}&select=*";
                        var gpReq = BuildAdminRequest(HttpMethod.Get, gpUrl);
                        var gpRes = await _http.SendAsync(gpReq);
                        var gpContent = await gpRes.Content.ReadAsStringAsync();
                        EnsureSuccess(gpRes, gpContent);
                        var gp = JsonSerializer.Deserialize<List<GuideProfileRow>>(gpContent, _json)?.FirstOrDefault();

                        if (gp != null)
                        {
                            decimal newRating = Math.Max(0.00m, gp.AverageRating - 0.50m);
                            bool newVerified = newRating >= 3.00m ? gp.IsVerified : false;
                            
                            var gpUpdates = new { average_rating = newRating, is_verified = newVerified, updated_at = DateTime.UtcNow };
                            var gpPatchReq = BuildAdminRequest(HttpMethod.Patch, $"{_supabaseUrl}/rest/v1/guide_profiles?id=eq.{booking.GuideProfileId}");
                            gpPatchReq.Content = new StringContent(JsonSerializer.Serialize(gpUpdates), Encoding.UTF8, "application/json");
                            var gpPatchRes = await _http.SendAsync(gpPatchReq);
                            EnsureSuccess(gpPatchRes, await gpPatchRes.Content.ReadAsStringAsync());

                            // Notify Guide
                            var guideUserId = await GetGuideUserIdAsync(booking.GuideProfileId);
                            if (!string.IsNullOrEmpty(guideUserId))
                            {
                                await _notif.SendAsync(
                                    guideUserId,
                                    NotificationTypes.PayoutFailed,
                                    "Penalty applied for No-Show",
                                    $"A penalty has been applied to your profile due to a No-Show for booking {bookingId}. Your rating has been reduced by 0.5.",
                                    new { bookingId },
                                    "/Guide/Support",
                                    $"guide-penalty:{bookingId}",
                                    sendEmail: true);
                            }
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(booking.TravelerId))
                    {
                        await _notif.SendAsync(
                            booking.TravelerId,
                            NotificationTypes.RefundProcessed,
                            "Cancellation and refund approved",
                            $"The cancellation for booking {bookingId} was approved and the refund was recorded.",
                            new { bookingId, amount = booking.AmountPaid },
                            $"/Traveler/BookingDetails/{bookingId}",
                            $"refund-processed:{bookingId}",
                            sendEmail: true);
                    }
                }
                else if (!string.IsNullOrWhiteSpace(booking.TravelerId))
                {
                    await _notif.SendAsync(
                        booking.TravelerId,
                        NotificationTypes.BookingConfirmed,
                        "Cancellation request declined",
                        $"Booking {bookingId} remains confirmed.",
                        new { bookingId },
                        $"/Traveler/BookingDetails/{bookingId}",
                        $"cancellation-declined:{bookingId}");
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving cancellation request");
                return false;
            }
        }

        // Reassign guide for booking
        public async Task<bool> ReassignGuideAsync(string bookingId, string newGuideProfileId)
        {
            try
            {
                // 1. Get booking info
                var bookingRequest = BuildAdminRequest(HttpMethod.Get, $"{_supabaseUrl}/rest/v1/bookings?id=eq.{bookingId}&select=*");
                var bookingResponse = await _http.SendAsync(bookingRequest);
                var bookingContent = await bookingResponse.Content.ReadAsStringAsync();
                EnsureSuccess(bookingResponse, bookingContent);
                var booking = JsonSerializer.Deserialize<List<BookingKpiRow>>(bookingContent, _json)?.FirstOrDefault();
                if (booking is null) return false;

                // 2. Fetch new guide's user_id and profile data (e.g. name)
                var gpUrl = $"{_supabaseUrl}/rest/v1/guide_profiles?id=eq.{newGuideProfileId}&select=user_id,profiles:user_id(full_name)";
                var gpReq = BuildAdminRequest(HttpMethod.Get, gpUrl);
                var gpRes = await _http.SendAsync(gpReq);
                var gpContent = await gpRes.Content.ReadAsStringAsync();
                EnsureSuccess(gpRes, gpContent);
                
                using var gpDoc = JsonDocument.Parse(gpContent);
                var gpRow = gpDoc.RootElement.EnumerateArray().FirstOrDefault();
                if (gpRow.ValueKind != JsonValueKind.Object) return false;
                
                var newGuideUserId = gpRow.GetProperty("user_id").GetString();
                var newGuideName = "a new Guide";
                if (gpRow.TryGetProperty("profiles", out var profilesObj) && profilesObj.ValueKind == JsonValueKind.Object)
                {
                    if (profilesObj.TryGetProperty("full_name", out var fnProp))
                    {
                        newGuideName = fnProp.GetString() ?? "a new Guide";
                    }
                }

                // 3. Update booking in DB
                var updates = new {
                    guide_profile_id = newGuideProfileId,
                    status = 1, // Confirmed
                    cancel_reason = (string?)null,
                    completion_state = "not_started",
                    updated_at = DateTime.UtcNow
                };
                var patchReq = BuildAdminRequest(HttpMethod.Patch, $"{_supabaseUrl}/rest/v1/bookings?id=eq.{bookingId}");
                patchReq.Content = new StringContent(JsonSerializer.Serialize(updates), Encoding.UTF8, "application/json");
                var patchRes = await _http.SendAsync(patchReq);
                EnsureSuccess(patchRes, await patchRes.Content.ReadAsStringAsync());

                // 4. Send notifications
                // A) Traveler
                if (!string.IsNullOrWhiteSpace(booking.TravelerId))
                {
                    await _notif.SendAsync(
                        booking.TravelerId,
                        NotificationTypes.BookingConfirmed,
                        "Guide Reassigned for your Trip",
                        $"Your booking {bookingId} has been reassigned to a new Hướng dẫn viên: {newGuideName}.",
                        new { bookingId, guideName = newGuideName },
                        $"/Traveler/BookingDetails/{bookingId}",
                        $"traveler-reassigned:{bookingId}");
                }

                // B) New Guide
                if (!string.IsNullOrEmpty(newGuideUserId))
                {
                    await _notif.SendAsync(
                        newGuideUserId,
                        NotificationTypes.BookingConfirmed,
                        "Urgent Reassigned Tour Request",
                        $"You have been assigned to handle booking {bookingId} by Admin. Please check your schedule.",
                        new { bookingId },
                        "/Guide/Bookings",
                        $"guide-reassigned:{bookingId}",
                        sendEmail: true);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reassigning guide for booking {BookingId}", bookingId);
                return false;
            }
        }

        private async Task<string?> GetGuideUserIdAsync(string? guideProfileId)
        {
            if (string.IsNullOrWhiteSpace(guideProfileId)) return null;
            var url = $"{_supabaseUrl}/rest/v1/guide_profiles?id=eq.{guideProfileId}&select=user_id&limit=1";
            var request = BuildAdminRequest(HttpMethod.Get, url);
            var response = await _http.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();
            EnsureSuccess(response, content);
            using var document = JsonDocument.Parse(content);
            var row = document.RootElement.EnumerateArray().FirstOrDefault();
            return row.ValueKind == JsonValueKind.Object && row.TryGetProperty("user_id", out var userId)
                ? userId.GetString()
                : null;
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

                var entries = JsonSerializer.Deserialize<List<LedgerRow>>(content, _json) ?? new();

                if (entries.Any())
                {
                    var userIds = entries
                        .Select(e => e.UserId)
                        .Where(id => !string.IsNullOrEmpty(id))
                        .Distinct()
                        .ToList();

                    if (userIds.Any())
                    {
                        var idString = string.Join(",", userIds);
                        var profilesUrl = $"{_supabaseUrl}/rest/v1/profiles?id=in.({idString})&select=id,full_name,email";
                        var profilesRequest = BuildAdminRequest(HttpMethod.Get, profilesUrl);
                        var profilesResponse = await _http.SendAsync(profilesRequest);
                        var profilesContent = await profilesResponse.Content.ReadAsStringAsync();

                        if (profilesResponse.IsSuccessStatusCode)
                        {
                            var profiles = JsonSerializer.Deserialize<List<ProfileData>>(profilesContent, _json) ?? new();
                            var profileMap = profiles
                                .Where(p => p.Id != null)
                                .ToDictionary(p => p.Id!, p => p, StringComparer.OrdinalIgnoreCase);

                            foreach (var e in entries)
                            {
                                if (e.UserId != null && profileMap.TryGetValue(e.UserId, out var p))
                                {
                                    e.Profile = p;
                                }
                            }
                        }
                    }
                }

                return entries;
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
                var url = $"{_supabaseUrl}/rest/v1/bookings?select=*,profiles:traveler_id(full_name,email),experience_packages(title,guide_profiles(profiles(full_name))),reviews(rating,comment)&order=created_at.desc";
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
                var url = $"{_supabaseUrl}/rest/v1/guide_profiles?select=*,profiles:user_id(full_name,email,is_active,avatar_url)&order=created_at.desc";
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
        [JsonPropertyName("completion_state")] public string? CompletionState { get; set; }
        [JsonPropertyName("total_amount")] public decimal TotalAmount { get; set; }
        [JsonPropertyName("amount_paid")] public decimal AmountPaid { get; set; }
        [JsonPropertyName("platform_fee")] public decimal PlatformFee { get; set; }
        [JsonPropertyName("guide_earnings")] public decimal GuideEarnings { get; set; }
        [JsonPropertyName("escrow_released")] public bool EscrowReleased { get; set; }
        [JsonPropertyName("payout_status")] public string? PayoutStatus { get; set; }
        [JsonPropertyName("cancel_reason")] public string? CancelReason { get; set; }
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
        [JsonPropertyName("profiles")] public ProfileData? Profile { get; set; }
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
        [JsonPropertyName("created_at")] public DateTime CreatedAt { get; set; }
        [JsonPropertyName("completion_state")] public string? CompletionState { get; set; }
        [JsonPropertyName("traveler_completed_at")] public DateTime? TravelerCompletedAt { get; set; }
        [JsonPropertyName("cancel_reason")] public string? CancelReason { get; set; }
        [JsonPropertyName("escrow_released")] public bool EscrowReleased { get; set; }
        [JsonPropertyName("payout_status")] public string? PayoutStatus { get; set; }
        [JsonPropertyName("profiles")] public ProfileData? Traveler { get; set; }
        [JsonPropertyName("experience_packages")] public BookingPackageJoined? Package { get; set; }
        [JsonPropertyName("reviews")] public AdminReviewInfo? Reviews { get; set; }
    }

    public class AdminReviewInfo
    {
        [JsonPropertyName("rating")] public int Rating { get; set; }
        [JsonPropertyName("comment")] public string? Comment { get; set; }
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
