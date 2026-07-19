using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace TripMate_WebAPI.Services;

/// <summary>
/// SurveyService — refactored to match database_setup.sql schema.
/// reviews table uses guide_profile_id (not tour_id).
/// bookings uses experience_package_id (not tour_id).
/// Rating recalculation updates guide_profiles.average_rating / total_reviews.
/// </summary>
public class SurveyService
{
    private readonly HttpClient _http;
    private readonly string _supabaseUrl;
    private readonly string _anonKey;
    private readonly INotificationService _notificationService;
    private readonly ILogger<SurveyService> _logger;

    public SurveyService(
        HttpClient http,
        IConfiguration config,
        INotificationService notificationService,
        ILogger<SurveyService> logger)
    {
        _http = http;
        _supabaseUrl = config["Supabase:Url"]!;
        _anonKey = config["Supabase:AnonKey"]!;
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Submit a review for a completed booking
    /// </summary>
    public async Task<SurveySubmissionResponse> SubmitSurveyAsync(
        SubmitSurveyRequest request,
        string userId,
        string userToken)
    {
        try
        {
            // Verify booking status is "completed" (status = 2)
            var booking = await GetBookingAsync(request.BookingId, userToken);
            if (booking == null)
            {
                return new SurveySubmissionResponse(false, "Booking not found", null, null);
            }

            if (booking.Status != 2) // 2 = completed
            {
                return new SurveySubmissionResponse(
                    false, "Chỉ có thể đánh giá tour đã hoàn thành", null, null);
            }

            // Verify no previous review for this booking
            var existingSurvey = await GetSurveyByBookingIdAsync(request.BookingId, userToken);
            if (existingSurvey != null)
            {
                return new SurveySubmissionResponse(
                    false, "Survey already submitted", null, null);
            }

            // Store review in database
            var survey = await CreateSurveyAsync(request, userId, userToken);
            if (survey == null)
            {
                return new SurveySubmissionResponse(
                    false, "Failed to create survey", null, null);
            }

            // Recalculate guide rating
            await RecalculateGuideRatingAsync(request.GuideProfileId, userToken);

            // Send notification to guide
            await SendGuideNotificationAsync(request.GuideProfileId, request.BookingId, userId, request.Rating, userToken);

            // Check for first-time survey and create voucher
            var voucher = await CreateFirstTimeSurveyVoucherAsync(userId, userToken);

            return new SurveySubmissionResponse(
                true, "Cảm ơn bạn đã đánh giá tour!", survey, voucher);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting survey");
            return new SurveySubmissionResponse(
                false, "Đã xảy ra lỗi khi gửi đánh giá", null, null);
        }
    }

    /// <summary>
    /// Get all published reviews for a guide
    /// </summary>
    public async Task<TourSurveysResponse> GetTourSurveysAsync(
        string guideProfileId, string userToken)
    {
        try
        {
            var url = $"{_supabaseUrl}/rest/v1/reviews" +
                      $"?guide_profile_id=eq.{guideProfileId}" +
                      $"&order=created_at.desc" +
                      $"&select=*";

            var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Add("apikey", _anonKey);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

            var res = await _http.SendAsync(req);
            var content = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch guide reviews: {Content}", content);
                return new TourSurveysResponse(new List<SurveyDto>(), 0, 0);
            }

            var rows = JsonSerializer.Deserialize<List<ReviewRow>>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

            var surveys = new List<SurveyDto>();
            foreach (var row in rows)
            {
                var travelerName = await GetTravelerNameAsync(row.TravelerId, userToken);
                surveys.Add(new SurveyDto(
                    row.Id,
                    row.GuideProfileId,
                    row.TravelerId,
                    travelerName,
                    row.BookingId,
                    row.Rating,
                    row.Comment ?? "",
                    true,
                    row.CreatedAt
                ));
            }

            var avgRating = surveys.Any() ? surveys.Average(s => s.Rating) : 0;
            return new TourSurveysResponse(surveys, surveys.Count, Math.Round(avgRating, 1));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting guide reviews");
            return new TourSurveysResponse(new List<SurveyDto>(), 0, 0);
        }
    }

    /// <summary>
    /// Get traveler's review history
    /// </summary>
    public async Task<TravelerSurveysResponse> GetTravelerSurveysAsync(
        string travelerId, string userToken)
    {
        try
        {
            var url = $"{_supabaseUrl}/rest/v1/reviews" +
                      $"?traveler_id=eq.{travelerId}" +
                      $"&order=created_at.desc" +
                      $"&select=*";

            var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Add("apikey", _anonKey);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

            var res = await _http.SendAsync(req);
            var content = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch traveler reviews: {Content}", content);
                return new TravelerSurveysResponse(new List<TravelerSurveyDto>(), 0);
            }

            var rows = JsonSerializer.Deserialize<List<ReviewRow>>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

            var surveys = new List<TravelerSurveyDto>();
            foreach (var row in rows)
            {
                var guideInfo = await GetGuideBasicInfoAsync(row.GuideProfileId, userToken);
                surveys.Add(new TravelerSurveyDto(
                    row.Id,
                    row.GuideProfileId,
                    guideInfo?.FullName ?? "Unknown Guide",
                    guideInfo?.CityArea ?? "",
                    row.Rating,
                    row.Comment ?? "",
                    row.CreatedAt
                ));
            }

            return new TravelerSurveysResponse(surveys, surveys.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting traveler reviews");
            return new TravelerSurveysResponse(new List<TravelerSurveyDto>(), 0);
        }
    }

    /// <summary>
    /// Get survey analytics for admin dashboard
    /// </summary>
    public async Task<SurveyAnalyticsDto> GetSurveyAnalyticsAsync(string userToken)
    {
        try
        {
            // Get all reviews
            var url = $"{_supabaseUrl}/rest/v1/reviews?select=*";
            var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Add("apikey", _anonKey);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

            var res = await _http.SendAsync(req);
            var content = await res.Content.ReadAsStringAsync();

            var reviews = JsonSerializer.Deserialize<List<ReviewRow>>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

            // Get all completed bookings count (status = 2)
            var bookingsUrl = $"{_supabaseUrl}/rest/v1/bookings?status=eq.2&select=count";
            var bookingsReq = new HttpRequestMessage(HttpMethod.Get, bookingsUrl);
            bookingsReq.Headers.Add("apikey", _anonKey);
            bookingsReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
            bookingsReq.Headers.Add("Prefer", "count=exact");

            var bookingsRes = await _http.SendAsync(bookingsReq);
            var totalCompletedBookings = 0;
            if (bookingsRes.Headers.TryGetValues("Content-Range", out var values))
            {
                var range = values.FirstOrDefault();
                if (range != null && range.Contains('/'))
                {
                    var parts = range.Split('/');
                    int.TryParse(parts[1], out totalCompletedBookings);
                }
            }

            // Calculate metrics
            var totalSurveys = reviews.Count;
            var avgRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;
            var submissionRate = totalCompletedBookings > 0
                ? (double)totalSurveys / totalCompletedBookings * 100
                : 0;

            var ratingDistribution = reviews
                .GroupBy(r => r.Rating)
                .ToDictionary(g => g.Key, g => g.Count());

            // Highest and lowest rated guides
            var guideRatings = reviews
                .GroupBy(r => r.GuideProfileId)
                .Select(g => new
                {
                    GuideProfileId = g.Key,
                    AvgRating = g.Average(r => r.Rating),
                    Count = g.Count()
                })
                .Where(t => t.Count >= 3)
                .ToList();

            TourRatingDto? highest = null;
            TourRatingDto? lowest = null;

            if (guideRatings.Any())
            {
                var highestGuide = guideRatings.OrderByDescending(t => t.AvgRating).First();
                var lowestGuide = guideRatings.OrderBy(t => t.AvgRating).First();

                var highestGuideInfo = await GetGuideBasicInfoAsync(highestGuide.GuideProfileId, userToken);
                var lowestGuideInfo = await GetGuideBasicInfoAsync(lowestGuide.GuideProfileId, userToken);

                if (highestGuideInfo != null)
                {
                    highest = new TourRatingDto(
                        highestGuide.GuideProfileId,
                        highestGuideInfo.FullName ?? "Unknown",
                        Math.Round(highestGuide.AvgRating, 1),
                        highestGuide.Count
                    );
                }

                if (lowestGuideInfo != null)
                {
                    lowest = new TourRatingDto(
                        lowestGuide.GuideProfileId,
                        lowestGuideInfo.FullName ?? "Unknown",
                        Math.Round(lowestGuide.AvgRating, 1),
                        lowestGuide.Count
                    );
                }
            }

            return new SurveyAnalyticsDto(
                totalSurveys,
                Math.Round(avgRating, 1),
                totalCompletedBookings,
                Math.Round(submissionRate, 1),
                highest,
                lowest,
                ratingDistribution
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting survey analytics");
            return new SurveyAnalyticsDto(0, 0, 0, 0, null, null, new Dictionary<int, int>());
        }
    }

    // ── Private Helper Methods ────────────────────────────────────────────────

    private async Task<SurveyBookingRow?> GetBookingAsync(string bookingId, string userToken)
    {
        try
        {
            var url = $"{_supabaseUrl}/rest/v1/bookings?id=eq.{bookingId}&select=*";
            var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Add("apikey", _anonKey);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

            var res = await _http.SendAsync(req);
            var content = await res.Content.ReadAsStringAsync();

            var bookings = JsonSerializer.Deserialize<List<SurveyBookingRow>>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return bookings?.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting booking");
            return null;
        }
    }

    private async Task<ReviewRow?> GetSurveyByBookingIdAsync(string bookingId, string userToken)
    {
        try
        {
            var url = $"{_supabaseUrl}/rest/v1/reviews?booking_id=eq.{bookingId}&select=*";
            var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Add("apikey", _anonKey);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

            var res = await _http.SendAsync(req);
            var content = await res.Content.ReadAsStringAsync();

            var reviews = JsonSerializer.Deserialize<List<ReviewRow>>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return reviews?.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existing review");
            return null;
        }
    }

    private async Task<SurveyDto?> CreateSurveyAsync(
        SubmitSurveyRequest request, string userId, string userToken)
    {
        try
        {
            var payload = new
            {
                guide_profile_id = request.GuideProfileId,
                traveler_id = userId,
                booking_id = request.BookingId,
                rating = request.Rating,
                comment = request.Comment
            };

            var req = new HttpRequestMessage(HttpMethod.Post,
                $"{_supabaseUrl}/rest/v1/reviews");
            req.Headers.Add("apikey", _anonKey);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
            req.Headers.Add("Prefer", "return=representation");
            req.Content = new StringContent(
                JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var res = await _http.SendAsync(req);
            var content = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to create review: {Content}", content);
                return null;
            }

            var rows = JsonSerializer.Deserialize<List<ReviewRow>>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var row = rows?.FirstOrDefault();
            if (row == null) return null;

            var travelerName = await GetTravelerNameAsync(userId, userToken);

            return new SurveyDto(
                row.Id, row.GuideProfileId, row.TravelerId, travelerName,
                row.BookingId, row.Rating, row.Comment ?? "", true, row.CreatedAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating review");
            return null;
        }
    }

    private async Task RecalculateGuideRatingAsync(string guideProfileId, string userToken)
    {
        try
        {
            // Get all reviews for this guide
            var url = $"{_supabaseUrl}/rest/v1/reviews?guide_profile_id=eq.{guideProfileId}&select=rating";
            var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Add("apikey", _anonKey);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

            var res = await _http.SendAsync(req);
            var content = await res.Content.ReadAsStringAsync();

            var reviews = JsonSerializer.Deserialize<List<ReviewRow>>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

            if (!reviews.Any()) return;

            var avgRating = Math.Round(reviews.Average(r => r.Rating), 1);
            var totalReviews = reviews.Count;

            // Update guide_profiles rating
            var updatePayload = new
            {
                average_rating = avgRating,
                total_reviews = totalReviews
            };

            var updateReq = new HttpRequestMessage(HttpMethod.Patch,
                $"{_supabaseUrl}/rest/v1/guide_profiles?id=eq.{guideProfileId}");
            updateReq.Headers.Add("apikey", _anonKey);
            updateReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
            updateReq.Content = new StringContent(
                JsonSerializer.Serialize(updatePayload), Encoding.UTF8, "application/json");

            await _http.SendAsync(updateReq);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalculating guide rating");
        }
    }

    private async Task SendGuideNotificationAsync(
        string guideProfileId, string bookingId, string travelerId, int rating, string userToken)
    {
        try
        {
            var guideInfo = await GetGuideBasicInfoAsync(guideProfileId, userToken);
            if (guideInfo == null) return;

            var travelerName = await GetTravelerNameAsync(travelerId, userToken);

            // Send notification to the guide's user_id
            await _notificationService.SendAsync(
                guideInfo.UserId ?? "",
                NotificationTypes.ReviewReceived,
                "New review received",
                $"{travelerName} rated you {rating} star(s).",
                new { guideProfileId, bookingId, rating },
                "/Guide/Profile",
                $"review:{bookingId}"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending guide notification");
        }
    }

    private async Task<DiscountVoucherDto?> CreateFirstTimeSurveyVoucherAsync(
        string userId, string userToken)
    {
        try
        {
            var url = $"{_supabaseUrl}/rest/v1/reviews?traveler_id=eq.{userId}&select=count";
            var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Add("apikey", _anonKey);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
            req.Headers.Add("Prefer", "count=exact");

            var res = await _http.SendAsync(req);

            var surveyCount = 0;
            if (res.Headers.TryGetValues("Content-Range", out var values))
            {
                var range = values.FirstOrDefault();
                if (range != null && range.Contains('/'))
                {
                    var parts = range.Split('/');
                    int.TryParse(parts[1], out surveyCount);
                }
            }

            if (surveyCount == 1)
            {
                var voucherCode = $"FIRST5-{Guid.NewGuid().ToString()[..8].ToUpper()}";
                var expiresAt = DateTime.UtcNow.AddDays(30);

                await _notificationService.SendAsync(
                    userId,
                    NotificationTypes.VoucherIssued,
                    "Your 5% voucher is ready",
                    $"Thanks for your first review. Use {voucherCode} on your next booking.",
                    new { voucherCode, discountPercent = 5, expiresAt },
                    "/Home/Tours",
                    $"voucher:{voucherCode}"
                );

                return new DiscountVoucherDto(voucherCode, 5, expiresAt);
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating first-time voucher");
            return null;
        }
    }

    private async Task<string> GetTravelerNameAsync(string travelerId, string userToken)
    {
        try
        {
            var url = $"{_supabaseUrl}/rest/v1/profiles?id=eq.{travelerId}&select=full_name";
            var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Add("apikey", _anonKey);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

            var res = await _http.SendAsync(req);
            var content = await res.Content.ReadAsStringAsync();

            var profiles = JsonSerializer.Deserialize<List<SurveyProfileRow>>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return profiles?.FirstOrDefault()?.FullName ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    private async Task<GuideBasicInfo?> GetGuideBasicInfoAsync(
        string guideProfileId, string userToken)
    {
        try
        {
            var url = $"{_supabaseUrl}/rest/v1/guide_profiles" +
                      $"?id=eq.{guideProfileId}" +
                      $"&select=id,user_id,city_area,profiles(full_name)";
            var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Add("apikey", _anonKey);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

            var res = await _http.SendAsync(req);
            var content = await res.Content.ReadAsStringAsync();

            var guides = JsonSerializer.Deserialize<List<GuideBasicInfoRow>>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var guide = guides?.FirstOrDefault();
            if (guide == null) return null;

            return new GuideBasicInfo(
                guide.Id, guide.UserId,
                guide.Profile?.FullName ?? "Unknown",
                guide.CityArea ?? ""
            );
        }
        catch
        {
            return null;
        }
    }
}

// ── Internal Models ───────────────────────────────────────────────────────────

/// <summary>
/// Maps to public.reviews table in database_setup.sql
/// </summary>
internal class ReviewRow
{
    [JsonPropertyName("id")]               public string Id { get; set; } = "";
    [JsonPropertyName("guide_profile_id")] public string GuideProfileId { get; set; } = "";
    [JsonPropertyName("traveler_id")]      public string TravelerId { get; set; } = "";
    [JsonPropertyName("booking_id")]       public string? BookingId { get; set; }
    [JsonPropertyName("rating")]           public int Rating { get; set; }
    [JsonPropertyName("comment")]          public string? Comment { get; set; }
    [JsonPropertyName("created_at")]       public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Booking row for survey validation.
/// Matches public.bookings schema — status is smallint (0-3).
/// </summary>
internal class SurveyBookingRow
{
    [JsonPropertyName("id")]                    public string Id { get; set; } = "";
    [JsonPropertyName("status")]                public int Status { get; set; }
    [JsonPropertyName("experience_package_id")] public string ExperiencePackageId { get; set; } = "";
    [JsonPropertyName("traveler_id")]           public string TravelerId { get; set; } = "";
    [JsonPropertyName("guide_profile_id")]      public string GuideProfileId { get; set; } = "";
}

internal class SurveyProfileRow
{
    [JsonPropertyName("id")]        public string Id { get; set; } = "";
    [JsonPropertyName("full_name")] public string FullName { get; set; } = "";
}

internal class GuideBasicInfoRow
{
    [JsonPropertyName("id")]        public string Id { get; set; } = "";
    [JsonPropertyName("user_id")]   public string UserId { get; set; } = "";
    [JsonPropertyName("city_area")] public string? CityArea { get; set; }
    [JsonPropertyName("profiles")]  public SurveyProfileRow? Profile { get; set; }
}

internal record GuideBasicInfo(string Id, string UserId, string FullName, string CityArea);
