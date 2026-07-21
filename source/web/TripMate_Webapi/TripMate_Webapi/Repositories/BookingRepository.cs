using Supabase;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Text.Json;
using TripMate_Webapi.Entities;
using TripMate_Webapi.Repositories.Models;

namespace TripMate_Webapi.Repositories
{
    public class BookingRepository : IBookingRepository
    {
        private readonly Client _supabase;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _supabaseUrl;
        private readonly string _apiKey;
        private static readonly JsonSerializerOptions CalendarJsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public BookingRepository(Client supabase, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _supabase = supabase;
            _httpClientFactory = httpClientFactory;
            _supabaseUrl = configuration["Supabase:Url"]
                ?? Environment.GetEnvironmentVariable("SUPABASE_URL")
                ?? throw new InvalidOperationException("Supabase URL is not configured.");
            _apiKey = Environment.GetEnvironmentVariable("SUPABASE_SERVICE_ROLE_KEY")
                ?? configuration["Supabase:AnonKey"]
                ?? Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY")
                ?? throw new InvalidOperationException("Supabase API key is not configured.");
        }

        public async Task<BookingEntity> CreateBookingAsync(BookingEntity booking, string? userToken = null)
        {
            var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL");
            var anonKey = Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY");
            var serviceKey = Environment.GetEnvironmentVariable("SUPABASE_SERVICE_ROLE_KEY");
            var tokenToUse = userToken ?? serviceKey ?? anonKey;
            
            using var http = new System.Net.Http.HttpClient();
            var req = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Post, $"{supabaseUrl}/rest/v1/bookings");
            req.Headers.Add("apikey", anonKey);
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenToUse);
            req.Headers.Add("Prefer", "return=representation");
            
            var bodyObj = new System.Collections.Generic.Dictionary<string, object?>
            {
                { "id", booking.Id },
                { "traveler_id", booking.TravelerId },
                { "guide_profile_id", booking.GuideProfileId },
                { "booking_date", booking.BookingDate.ToString("yyyy-MM-dd") },
                { "start_time", booking.StartTime.ToString("HH:mm:ssZ") },
                { "guest_count", booking.GuestCount },
                { "total_amount", booking.TotalAmount },
                { "platform_fee", booking.PlatformFee },
                { "guide_earnings", booking.GuideEarnings },
                { "status", booking.Status },
                { "traveler_notes", booking.TravelerNotes },
                { "experience_package_id", booking.ExperiencePackageId }
            };
            
            var json = System.Text.Json.JsonSerializer.Serialize(bodyObj);
            req.Content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await http.SendAsync(req);
            if (response.IsSuccessStatusCode)
            {
                return booking;
            }
            else 
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to insert booking: {error}");
            }
        }

        public async Task<List<BookingEntity>> GetBookingsByTravelerAsync(string travelerId)
        {
            var response = await _supabase.From<BookingEntity>()
                .Where(b => b.TravelerId == travelerId)
                .Get();
                
            return response.Models;
        }

        public async Task<BookingEntity?> GetBookingByIdAsync(string id)
        {
            var response = await _supabase.From<BookingEntity>()
                .Where(b => b.Id == id)
                .Single();
                
            return response;
        }

        public async Task<BookingEntity> UpdateBookingAsync(BookingEntity booking)
        {
            var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL");
            var anonKey = Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY");
            var serviceKey = Environment.GetEnvironmentVariable("SUPABASE_SERVICE_ROLE_KEY");
            var tokenToUse = serviceKey ?? anonKey;
            
            using var http = new System.Net.Http.HttpClient();
            var req = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Patch, $"{supabaseUrl}/rest/v1/bookings?id=eq.{booking.Id}");
            req.Headers.Add("apikey", anonKey);
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenToUse);
            req.Headers.Add("Prefer", "return=representation");
            
            var bodyObj = new System.Collections.Generic.Dictionary<string, object?>
            {
                { "status", booking.Status },
                { "amount_paid", booking.AmountPaid },
                { "updated_at", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") }
                // Only send the fields we actually update during PaymentCallback/Updates
                // If we need to update other fields later, we add them here.
                // We omit start_time so we don't trigger the "invalid time format" error.
            };
            
            var json = System.Text.Json.JsonSerializer.Serialize(bodyObj);
            req.Content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await http.SendAsync(req);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to update booking: {error}");
            }
            return booking;
        }

        public async Task<int> GetPendingBookingsCountAsync(string guideProfileId)
        {
            var responseWindowStartUtc = DateTime.UtcNow
                .AddHours(-24)
                .ToString("yyyy-MM-ddTHH:mm:ss.fffZ");

            var response = await _supabase.From<BookingEntity>()
                .Where(b => b.GuideProfileId == guideProfileId && b.Status == 0) // Status 0 = Pending
                .Filter("created_at", Postgrest.Constants.Operator.GreaterThan, responseWindowStartUtc)
                .Count(Postgrest.Constants.CountType.Exact);
                
            return response;
        }

        public async Task<string?> GetAnyTravelerProfileIdAsync()
        {
            var response = await _supabase.From<ProfileEntity>()
                .Where(p => p.Role == "traveler")
                .Get();
            return response.Models.FirstOrDefault()?.Id;
        }

        public async Task<List<BookingEntity>> GetGuideBookingsInRangeAsync(string guideProfileId, string start, string endExclusive)
        {
            var response = await _supabase.From<BookingEntity>()
                .Filter("guide_profile_id", Postgrest.Constants.Operator.Equals, guideProfileId)
                .Filter("booking_date", Postgrest.Constants.Operator.GreaterThanOrEqual, start)
                .Filter("booking_date", Postgrest.Constants.Operator.LessThan, endExclusive)
                .Get();
            return response.Models;
        }

        public async Task<List<CalendarBookingRecord>> GetGuideCalendarBookingsInRangeAsync(
            string guideProfileId,
            string start,
            string endExclusive)
        {
            var encodedGuideId = Uri.EscapeDataString(guideProfileId);
            var encodedStart = Uri.EscapeDataString(start);
            var encodedEnd = Uri.EscapeDataString(endExclusive);
            var select = string.Join(',', new[]
            {
                "id",
                "traveler_id",
                "experience_package_id",
                "booking_date",
                "start_time",
                "guest_count",
                "guide_earnings",
                "status",
                "traveler_notes",
                "created_at",
                "traveler:traveler_id(full_name,avatar_url)",
                "experience_package:experience_package_id(title,duration_hours,meeting_point,cover_image_url)"
            });

            var url = $"{_supabaseUrl}/rest/v1/bookings" +
                      $"?guide_profile_id=eq.{encodedGuideId}" +
                      $"&booking_date=gte.{encodedStart}" +
                      $"&booking_date=lt.{encodedEnd}" +
                      "&order=booking_date.asc,start_time.asc" +
                      $"&select={select}";

            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("apikey", _apiKey);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var client = _httpClientFactory.CreateClient();
            using var response = await client.SendAsync(request);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Failed to load calendar bookings: {content}");
            }

            return JsonSerializer.Deserialize<List<CalendarBookingRecord>>(content, CalendarJsonOptions) ?? [];
        }
        public async Task<List<BookingEntity>> GetBookingsForGuideAsync(string guideProfileId)
        {
            var response = await _supabase.From<BookingEntity>()
                .Where(b => b.GuideProfileId == guideProfileId)
                .Order(b => b.CreatedAt, Postgrest.Constants.Ordering.Descending)
                .Get();
                
            return response.Models;
        }

        public async Task UpdateBookingStatusAsync(string bookingId, int status)
        {
            var supabaseUrl = Environment.GetEnvironmentVariable("SUPABASE_URL");
            var anonKey = Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY");
            var serviceKey = Environment.GetEnvironmentVariable("SUPABASE_SERVICE_ROLE_KEY");
            var tokenToUse = serviceKey ?? anonKey;
            
            using var http = new System.Net.Http.HttpClient();
            var req = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Patch, $"{supabaseUrl}/rest/v1/bookings?id=eq.{bookingId}");
            req.Headers.Add("apikey", anonKey);
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenToUse);
            req.Headers.Add("Prefer", "return=representation");
            
            var bodyObj = new System.Collections.Generic.Dictionary<string, object?>
            {
                { "status", status },
                { "updated_at", DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ") }
            };
            
            var json = System.Text.Json.JsonSerializer.Serialize(bodyObj);
            req.Content = new System.Net.Http.StringContent(json, System.Text.Encoding.UTF8, "application/json");
            
            var response = await http.SendAsync(req);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to update booking status: {error}");
            }
        }

        public async Task DeleteBookingAsync(string id)
        {
            await _supabase.From<BookingEntity>()
                .Where(b => b.Id == id)
                .Delete();
        }
    }
}
