using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Supabase;
using TripMate_Webapi.Entities;

namespace TripMate_Webapi.Repositories
{
    /// <summary>
    /// M5: Repository cho bảng reviews.
    /// Dùng Supabase REST API trực tiếp (giống BookingRepository) để bypass RLS khi cần.
    /// </summary>
    public class ReviewRepository : IReviewRepository
    {
        private readonly Client _supabase;
        private readonly string _supabaseUrl;
        private readonly string _anonKey;
        private readonly string? _serviceKey;

        private static readonly JsonSerializerOptions _json = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        public ReviewRepository(Client supabase, IConfiguration config)
        {
            _supabase = supabase;
            _supabaseUrl = config["Supabase:Url"]
                ?? Environment.GetEnvironmentVariable("SUPABASE_URL")
                ?? "";
            _anonKey = config["Supabase:AnonKey"]
                ?? Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY")
                ?? "";
            _serviceKey = config["Supabase:ServiceRoleKey"]
                ?? Environment.GetEnvironmentVariable("SUPABASE_SERVICE_ROLE_KEY");
        }

        // ── Tạo review mới ────────────────────────────────────────────────────

        public async Task<ReviewEntity> CreateReviewAsync(ReviewEntity review)
        {
            var tokenToUse = _serviceKey ?? _anonKey;

            using var http = new HttpClient();
            var req = new HttpRequestMessage(HttpMethod.Post, $"{_supabaseUrl}/rest/v1/reviews");
            req.Headers.Add("apikey", _anonKey);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenToUse);
            req.Headers.Add("Prefer", "return=representation");

            var body = new
            {
                id = review.Id,
                booking_id = review.BookingId,
                traveler_id = review.TravelerId,
                guide_profile_id = review.GuideProfileId,
                rating = review.Rating,
                comment = review.Comment,
                created_at = DateTime.UtcNow
            };

            req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            var response = await http.SendAsync(req);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Failed to create review: {response.StatusCode} — {content}");

            var rows = JsonSerializer.Deserialize<List<ReviewEntity>>(content, _json);
            return rows?.FirstOrDefault() ?? review;
        }

        // ── Lấy reviews theo guide ────────────────────────────────────────────

        public async Task<List<ReviewEntity>> GetReviewsByGuideAsync(string guideProfileId)
        {
            var response = await _supabase.From<ReviewEntity>()
                .Filter("guide_profile_id", Postgrest.Constants.Operator.Equals, guideProfileId)
                .Order(r => r.CreatedAt, Postgrest.Constants.Ordering.Descending)
                .Get();

            return response.Models;
        }

        // ── Kiểm tra duplicate review ─────────────────────────────────────────

        public async Task<bool> HasReviewForBookingAsync(string bookingId)
        {
            var response = await _supabase.From<ReviewEntity>()
                .Where(r => r.BookingId == bookingId)
                .Get();

            return response.Models.Any();
        }

        // ── Lấy toàn bộ reviews ────────────────────────────────────────────────
        public async Task<List<ReviewEntity>> GetAllReviewsAsync()
        {
            var response = await _supabase.From<ReviewEntity>()
                .Select("*, traveler:profiles(*), guide:guide_profiles(*, profile:profiles(*))")
                .Order(r => r.CreatedAt, Postgrest.Constants.Ordering.Descending)
                .Limit(50) // Limit to avoid pulling too much data
                .Get();

            return response.Models;
        }
    }
}
