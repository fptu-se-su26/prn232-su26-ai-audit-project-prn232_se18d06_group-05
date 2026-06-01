using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TripMate_WebAPI.Models;

namespace TripMate_WebAPI.Services;

public class SurveyService
{
    private readonly HttpClient _http;
    private readonly string _supabaseUrl;
    private readonly string _anonKey;
    private readonly NotificationService _notificationService;
    private readonly ILogger<SurveyService> _logger;

    public SurveyService(
        HttpClient http,
        IConfiguration config,
        NotificationService notificationService,
        ILogger<SurveyService> logger)
    {
        _http = http;
        _supabaseUrl = config["Supabase:Url"]!;
        _anonKey = config["Supabase:AnonKey"]!;
        _notificationService = notificationService;
        _logger = logger;
    }

    /// <summary>
    /// Submit a survey for a completed tour booking
    /// Implements Requirements 1, 2, 5, 6, 9
    /// </summary>
    public async Task<SurveySubmissionResponse> SubmitSurveyAsync(
        SubmitSurveyRequest request,
        string userId,
        string userToken)
    {
        try
        {
            // Requirement 1.4: Verify booking status is "completed"
            var booking = await GetBookingAsync(request.BookingId, userToken);
            if (booking == null)
            {
                return new SurveySubmissionResponse(
                    false,
                    "Booking not found",
                    null,
                    null
                );
            }

            if (booking.Status != "completed")
            {
                return new SurveySubmissionResponse(
                    false,
                    "Chỉ có thể đánh giá tour đã hoàn thành",
                    null,
                    null
                );
            }

            // Requirement 1.5: Verify no previous survey for this booking
            var existingSurvey = await GetSurveyByBookingIdAsync(request.BookingId, userToken);
            if (existingSurvey != null)
            {
                return new SurveySubmissionResponse(
                    false,
                    "Survey already submitted",
                    null,
                    null
                );
            }

            // Requirement 1.6: Store survey in database
            var survey = await CreateSurveyAsync(request, userId, userToken);
            if (survey == null)
            {
                return new SurveySubmissionResponse(
                    false,
                    "Failed to create survey",
                    null,
                    null
                );
            }

            // Requirement 1.7: Recalculate tour rating
            await RecalculateTourRatingAsync(request.TourId, userToken);

            // Requirement 5: Send notification to guide
            await SendGuideNotificationAsync(request.TourId, userId, request.Rating, userToken);

            // Requirement 9: Check for first-time survey and create voucher
            var voucher = await CreateFirstTimeSurveyVoucherAsync(userId, userToken);

            // Requirement 1.8: Return success response
            return new SurveySubmissionResponse(
                true,
                "Cảm ơn bạn đã đánh giá tour!",
                survey,
                voucher
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting survey");
            return new SurveySubmissionResponse(
                false,
                "Đã xảy ra lỗi khi gửi đánh giá",
                null,
                null
            );
        }
    }

    /// <summary>
    /// Get all published surveys for a tour
    /// Implements Requirement 4
    /// </summary>
    public async Task<TourSurveysResponse> GetTourSurveysAsync(
        string tourId,
        string userToken)
    {
        try
        {
            var url = $"{_supabaseUrl}/rest/v1/reviews" +
                      $"?tour_id=eq.{tourId}" +
                      $"&order=created_at.desc" +
                      $"&select=*";

            var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Add("apikey", _anonKey);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

            var res = await _http.SendAsync(req);
            var content = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch tour surveys: {Content}", content);
                return new TourSurveysResponse(new List<SurveyDto>(), 0, 0);
            }

            var rows = JsonSerializer.Deserialize<List<SurveyReviewRow>>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

            var surveys = new List<SurveyDto>();
            foreach (var row in rows)
            {
                var travelerName = await GetTravelerNameAsync(row.TravelerId, userToken);
                surveys.Add(new SurveyDto(
                    row.Id,
                    row.TourId,
                    row.TravelerId,
                    travelerName,
                    row.BookingId,
                    row.Rating,
                    row.Comment ?? "",
                    true, // All stored surveys are published
                    row.CreatedAt
                ));
            }

            var avgRating = surveys.Any() ? surveys.Average(s => s.Rating) : 0;

            return new TourSurveysResponse(surveys, surveys.Count, Math.Round(avgRating, 1));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting tour surveys");
            return new TourSurveysResponse(new List<SurveyDto>(), 0, 0);
        }
    }

    /// <summary>
    /// Get traveler's survey history
    /// Implements Requirement 8
    /// </summary>
    public async Task<TravelerSurveysResponse> GetTravelerSurveysAsync(
        string travelerId,
        string userToken)
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
                _logger.LogError("Failed to fetch traveler surveys: {Content}", content);
                return new TravelerSurveysResponse(new List<TravelerSurveyDto>(), 0);
            }

            var rows = JsonSerializer.Deserialize<List<SurveyReviewRow>>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

            var surveys = new List<TravelerSurveyDto>();
            foreach (var row in rows)
            {
                var tour = await GetTourBasicInfoAsync(row.TourId, userToken);
                surveys.Add(new TravelerSurveyDto(
                    row.Id,
                    row.TourId,
                    tour?.Title ?? "Unknown Tour",
                    tour?.Location ?? "",
                    row.Rating,
                    row.Comment ?? "",
                    row.CreatedAt
                ));
            }

            return new TravelerSurveysResponse(surveys, surveys.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting traveler surveys");
            return new TravelerSurveysResponse(new List<TravelerSurveyDto>(), 0);
        }
    }

    /// <summary>
    /// Get survey analytics for admin dashboard
    /// Implements Requirement 10
    /// </summary>
    public async Task<SurveyAnalyticsDto> GetSurveyAnalyticsAsync(string userToken)
    {
        try
        {
            // Get all surveys
            var url = $"{_supabaseUrl}/rest/v1/reviews?select=*";
            var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Add("apikey", _anonKey);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

            var res = await _http.SendAsync(req);
            var content = await res.Content.ReadAsStringAsync();

            var reviews = JsonSerializer.Deserialize<List<SurveyReviewRow>>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

            // Get all completed bookings
            var bookingsUrl = $"{_supabaseUrl}/rest/v1/bookings?status=eq.completed&select=count";
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

            // Rating distribution
            var ratingDistribution = reviews
                .GroupBy(r => r.Rating)
                .ToDictionary(g => g.Key, g => g.Count());

            // Highest and lowest rated tours
            var tourRatings = reviews
                .GroupBy(r => r.TourId)
                .Select(g => new
                {
                    TourId = g.Key,
                    AvgRating = g.Average(r => r.Rating),
                    Count = g.Count()
                })
                .Where(t => t.Count >= 3) // At least 3 reviews
                .ToList();

            TourRatingDto? highest = null;
            TourRatingDto? lowest = null;

            if (tourRatings.Any())
            {
                var highestTour = tourRatings.OrderByDescending(t => t.AvgRating).First();
                var lowestTour = tourRatings.OrderBy(t => t.AvgRating).First();

                var highestTourInfo = await GetTourBasicInfoAsync(highestTour.TourId, userToken);
                var lowestTourInfo = await GetTourBasicInfoAsync(lowestTour.TourId, userToken);

                if (highestTourInfo != null)
                {
                    highest = new TourRatingDto(
                        highestTour.TourId,
                        highestTourInfo.Title,
                        Math.Round(highestTour.AvgRating, 1),
                        highestTour.Count
                    );
                }

                if (lowestTourInfo != null)
                {
                    lowest = new TourRatingDto(
                        lowestTour.TourId,
                        lowestTourInfo.Title,
                        Math.Round(lowestTour.AvgRating, 1),
                        lowestTour.Count
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

    private async Task<SurveyReviewRow?> GetSurveyByBookingIdAsync(string bookingId, string userToken)
    {
        try
        {
            var url = $"{_supabaseUrl}/rest/v1/reviews?booking_id=eq.{bookingId}&select=*";
            var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Add("apikey", _anonKey);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

            var res = await _http.SendAsync(req);
            var content = await res.Content.ReadAsStringAsync();

            var reviews = JsonSerializer.Deserialize<List<SurveyReviewRow>>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return reviews?.FirstOrDefault();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existing survey");
            return null;
        }
    }

    private async Task<SurveyDto?> CreateSurveyAsync(
        SubmitSurveyRequest request,
        string userId,
        string userToken)
    {
        try
        {
            var payload = new
            {
                tour_id = request.TourId,
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
                JsonSerializer.Serialize(payload),
                Encoding.UTF8,
                "application/json");

            var res = await _http.SendAsync(req);
            var content = await res.Content.ReadAsStringAsync();

            if (!res.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to create survey: {Content}", content);
                return null;
            }

            var rows = JsonSerializer.Deserialize<List<SurveyReviewRow>>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            var row = rows?.FirstOrDefault();
            if (row == null) return null;

            var travelerName = await GetTravelerNameAsync(userId, userToken);

            return new SurveyDto(
                row.Id,
                row.TourId,
                row.TravelerId,
                travelerName,
                row.BookingId,
                row.Rating,
                row.Comment ?? "",
                true,
                row.CreatedAt
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating survey");
            return null;
        }
    }

    private async Task RecalculateTourRatingAsync(string tourId, string userToken)
    {
        try
        {
            // Get all reviews for this tour
            var url = $"{_supabaseUrl}/rest/v1/reviews?tour_id=eq.{tourId}&select=rating";
            var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Add("apikey", _anonKey);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

            var res = await _http.SendAsync(req);
            var content = await res.Content.ReadAsStringAsync();

            var reviews = JsonSerializer.Deserialize<List<SurveyReviewRow>>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];

            if (!reviews.Any()) return;

            var avgRating = Math.Round(reviews.Average(r => r.Rating), 1);
            var totalReviews = reviews.Count;

            // Update tour rating
            var updatePayload = new
            {
                rating = avgRating,
                total_reviews = totalReviews
            };

            var updateReq = new HttpRequestMessage(HttpMethod.Patch,
                $"{_supabaseUrl}/rest/v1/tours?id=eq.{tourId}");
            updateReq.Headers.Add("apikey", _anonKey);
            updateReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);
            updateReq.Content = new StringContent(
                JsonSerializer.Serialize(updatePayload),
                Encoding.UTF8,
                "application/json");

            await _http.SendAsync(updateReq);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recalculating tour rating");
        }
    }

    private async Task SendGuideNotificationAsync(
        string tourId,
        string travelerId,
        int rating,
        string userToken)
    {
        try
        {
            // Get tour and guide info
            var tour = await GetTourBasicInfoAsync(tourId, userToken);
            if (tour == null) return;

            var travelerName = await GetTravelerNameAsync(travelerId, userToken);

            // Send notification to guide
            await _notificationService.SendAsync(
                tour.GuideId,
                "new_review",
                "Đánh giá mới",
                $"{travelerName} đã đánh giá tour \"{tour.Title}\" với {rating} sao",
                new { tour_id = tourId, rating }
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending guide notification");
        }
    }

    private async Task<DiscountVoucherDto?> CreateFirstTimeSurveyVoucherAsync(
        string userId,
        string userToken)
    {
        try
        {
            // Check if this is first survey
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

            // If this is the first survey, create voucher
            if (surveyCount == 1)
            {
                var voucherCode = $"FIRST5-{Guid.NewGuid().ToString()[..8].ToUpper()}";
                var expiresAt = DateTime.UtcNow.AddDays(30);

                // TODO: Store voucher in database (vouchers table needs to be created)
                // For now, just return the voucher info

                await _notificationService.SendAsync(
                    userId,
                    "discount_voucher",
                    "Mã giảm giá cho bạn!",
                    $"Cảm ơn bạn đã đánh giá tour đầu tiên! Sử dụng mã {voucherCode} để được giảm 5% cho booking tiếp theo.",
                    new { voucher_code = voucherCode, discount_percent = 5, expires_at = expiresAt }
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

    private async Task<SurveyTourBasicInfo?> GetTourBasicInfoAsync(string tourId, string userToken)
    {
        try
        {
            var url = $"{_supabaseUrl}/rest/v1/tours?id=eq.{tourId}&select=id,guide_id,title,location";
            var req = new HttpRequestMessage(HttpMethod.Get, url);
            req.Headers.Add("apikey", _anonKey);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", userToken);

            var res = await _http.SendAsync(req);
            var content = await res.Content.ReadAsStringAsync();

            var tours = JsonSerializer.Deserialize<List<SurveyTourBasicInfo>>(content,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return tours?.FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }
}

// ── Internal Models ───────────────────────────────────────────────────────────

internal class SurveyReviewRow
{
    [JsonPropertyName("id")]          public string Id { get; set; } = "";
    [JsonPropertyName("tour_id")]     public string TourId { get; set; } = "";
    [JsonPropertyName("traveler_id")] public string TravelerId { get; set; } = "";
    [JsonPropertyName("booking_id")]  public string? BookingId { get; set; }
    [JsonPropertyName("rating")]      public int Rating { get; set; }
    [JsonPropertyName("comment")]     public string? Comment { get; set; }
    [JsonPropertyName("created_at")]  public DateTime CreatedAt { get; set; }
}

internal class SurveyBookingRow
{
    [JsonPropertyName("id")]          public string Id { get; set; } = "";
    [JsonPropertyName("status")]      public string Status { get; set; } = "";
    [JsonPropertyName("tour_id")]     public string TourId { get; set; } = "";
    [JsonPropertyName("traveler_id")] public string TravelerId { get; set; } = "";
}

internal class SurveyProfileRow
{
    [JsonPropertyName("id")]        public string Id { get; set; } = "";
    [JsonPropertyName("full_name")] public string FullName { get; set; } = "";
}

internal class SurveyTourBasicInfo
{
    [JsonPropertyName("id")]       public string Id { get; set; } = "";
    [JsonPropertyName("guide_id")] public string GuideId { get; set; } = "";
    [JsonPropertyName("title")]    public string Title { get; set; } = "";
    [JsonPropertyName("location")] public string Location { get; set; } = "";
}
