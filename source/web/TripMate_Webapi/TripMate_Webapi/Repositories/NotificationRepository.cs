using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Supabase;
using TripMate_Webapi.Entities;

namespace TripMate_Webapi.Repositories
{
    public class NotificationRepository : INotificationRepository
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

        public NotificationRepository(Client supabase, IConfiguration config)
        {
            _supabase = supabase;
            _supabaseUrl = config["Supabase:Url"] ?? Environment.GetEnvironmentVariable("SUPABASE_URL") ?? "";
            _anonKey = config["Supabase:AnonKey"] ?? Environment.GetEnvironmentVariable("SUPABASE_ANON_KEY") ?? "";
            _serviceKey = config["Supabase:ServiceRoleKey"] ?? Environment.GetEnvironmentVariable("SUPABASE_SERVICE_ROLE_KEY");
        }

        public async Task<NotificationEntity> CreateNotificationAsync(NotificationEntity notification)
        {
            var tokenToUse = _serviceKey ?? _anonKey;
            using var http = new HttpClient();
            var req = new HttpRequestMessage(HttpMethod.Post, $"{_supabaseUrl}/rest/v1/notifications");
            req.Headers.Add("apikey", _anonKey);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenToUse);
            req.Headers.Add("Prefer", "return=representation");

            var body = new
            {
                id = notification.Id,
                user_id = notification.UserId,
                title = notification.Title,
                message = notification.Message,
                type = notification.Type,
                is_read = notification.IsRead,
                link_url = notification.LinkUrl,
                created_at = DateTime.UtcNow
            };

            req.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

            var response = await http.SendAsync(req);
            var content = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Failed to create notification: {response.StatusCode} - {content}");

            var rows = JsonSerializer.Deserialize<List<NotificationEntity>>(content, _json);
            return rows?.FirstOrDefault() ?? notification;
        }

        public async Task<List<NotificationEntity>> GetNotificationsByUserIdAsync(string userId, int limit = 20)
        {
            var response = await _supabase.From<NotificationEntity>()
                .Where(n => n.UserId == userId)
                .Order(n => n.CreatedAt, Postgrest.Constants.Ordering.Descending)
                .Limit(limit)
                .Get();
            return response.Models;
        }

        public async Task<int> GetUnreadCountAsync(string userId)
        {
            var count = await _supabase.From<NotificationEntity>()
                .Where(n => n.UserId == userId)
                .Where(n => n.IsRead == false)
                .Count(Postgrest.Constants.CountType.Exact);
            return count;
        }

        public async Task MarkAsReadAsync(string notificationId)
        {
            await _supabase.From<NotificationEntity>()
                .Where(n => n.Id == notificationId)
                .Set(n => n.IsRead, true)
                .Update();
        }

        public async Task MarkAllAsReadAsync(string userId)
        {
            await _supabase.From<NotificationEntity>()
                .Where(n => n.UserId == userId)
                .Where(n => n.IsRead == false)
                .Set(n => n.IsRead, true)
                .Update();
        }
    }
}
