using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace TripMate_WebAPI.Services
{
    public interface INotificationService
    {
        Task NotifyAdminNewGuideApplicationAsync(string guideId, string guideName, string guideEmail);
        Task<List<NotificationDto>> GetAdminNotificationsAsync(string adminToken);
        Task MarkNotificationAsReadAsync(string notificationId, string adminToken);
        
        // Legacy method for realtime notifications
        Task SendAsync(string userId, string type, string title, string message, object? data = null);
    }

    public class NotificationService : INotificationService
    {
        private readonly HttpClient _http;
        private readonly IEmailService _emailService;
        private readonly string _supabaseUrl;
        private readonly string _anonKey;
        private readonly ILogger<NotificationService> _logger;
        private readonly JsonSerializerOptions _json;

        public NotificationService(
            HttpClient http,
            IEmailService emailService,
            IConfiguration config,
            ILogger<NotificationService> logger)
        {
            _http = http;
            _emailService = emailService;
            _supabaseUrl = config["Supabase:Url"] ?? throw new Exception("Supabase URL not configured");
            _anonKey = config["Supabase:AnonKey"] ?? throw new Exception("Supabase Anon Key not configured");
            _logger = logger;
            _json = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        public async Task NotifyAdminNewGuideApplicationAsync(string guideId, string guideName, string guideEmail)
        {
            try
            {
                // 1. Create notification in database
                await CreateNotificationAsync(guideId, guideName, guideEmail);

                // 2. Send email to all admin users
                await NotifyAdminsByEmailAsync(guideName, guideEmail);

                _logger.LogInformation("Admin notification sent for guide application: {GuideId}", guideId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending admin notification for guide: {GuideId}", guideId);
            }
        }

        public async Task<List<NotificationDto>> GetAdminNotificationsAsync(string adminToken)
        {
            try
            {
                var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"{_supabaseUrl}/rest/v1/admin_notifications?select=*&order=created_at.desc&limit=20");

                request.Headers.Add("apikey", _anonKey);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

                var response = await _http.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to get admin notifications: {Content}", content);
                    return new List<NotificationDto>();
                }

                var notifications = JsonSerializer.Deserialize<List<NotificationDto>>(content, _json);
                return notifications ?? new List<NotificationDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting admin notifications");
                return new List<NotificationDto>();
            }
        }

        public async Task MarkNotificationAsReadAsync(string notificationId, string adminToken)
        {
            try
            {
                var updateData = new
                {
                    is_read = true,
                    read_at = DateTime.UtcNow
                };

                var request = new HttpRequestMessage(
                    HttpMethod.Patch,
                    $"{_supabaseUrl}/rest/v1/admin_notifications?id=eq.{notificationId}");

                request.Headers.Add("apikey", _anonKey);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(updateData),
                    Encoding.UTF8,
                    "application/json");

                var response = await _http.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to mark notification as read: {Content}", content);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification as read");
            }
        }

        private async Task CreateNotificationAsync(string guideId, string guideName, string guideEmail)
        {
            try
            {
                var notification = new
                {
                    id = Guid.NewGuid().ToString(),
                    type = "guide_application",
                    title = "Đơn đăng ký Hướng dẫn viên mới",
                    message = $"{guideName} ({guideEmail}) đã đăng ký làm hướng dẫn viên và cần được xét duyệt.",
                    guide_id = guideId,
                    guide_name = guideName,
                    guide_email = guideEmail,
                    is_read = false,
                    created_at = DateTime.UtcNow
                };

                var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    $"{_supabaseUrl}/rest/v1/admin_notifications");

                request.Headers.Add("apikey", _anonKey);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(notification),
                    Encoding.UTF8,
                    "application/json");

                var response = await _http.SendAsync(request);

                if (!response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    _logger.LogError("Failed to create notification: {Content}", content);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating notification");
            }
        }

        private async Task NotifyAdminsByEmailAsync(string guideName, string guideEmail)
        {
            try
            {
                // Get all admin users
                var admins = await GetAdminUsersAsync();

                foreach (var admin in admins)
                {
                    if (!string.IsNullOrEmpty(admin.Email))
                    {
                        await _emailService.SendAdminNotificationEmailAsync(
                            admin.Email,
                            admin.FullName ?? "Admin",
                            guideName,
                            guideEmail);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending admin notification emails");
            }
        }

        private async Task<List<AdminUserDto>> GetAdminUsersAsync()
        {
            try
            {
                var request = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"{_supabaseUrl}/rest/v1/profiles?role=eq.admin&select=id,email,full_name");

                request.Headers.Add("apikey", _anonKey);

                var response = await _http.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to get admin users: {Content}", content);
                    return new List<AdminUserDto>();
                }

                var admins = JsonSerializer.Deserialize<List<AdminUserDto>>(content, _json);
                return admins ?? new List<AdminUserDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting admin users");
                return new List<AdminUserDto>();
            }
        }

        // Legacy method for realtime notifications - implementing for compatibility
        public async Task SendAsync(string userId, string type, string title, string message, object? data = null)
        {
            try
            {
                // For now, just log the notification - can be extended to use SignalR or other realtime service
                _logger.LogInformation("Realtime notification sent to user {UserId}: {Type} - {Title}", userId, type, title);
                
                // Could implement SignalR hub connection here if needed
                // await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", new { type, title, message, data });
                
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending realtime notification to user {UserId}", userId);
            }
        }
    }


}