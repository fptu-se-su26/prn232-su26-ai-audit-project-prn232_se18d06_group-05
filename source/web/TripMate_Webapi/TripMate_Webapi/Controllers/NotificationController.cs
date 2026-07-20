using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TripMate_Webapi.Repositories;

namespace TripMate_Webapi.Controllers
{
    public class NotificationController : Controller
    {
        private readonly INotificationRepository _notificationRepo;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(INotificationRepository notificationRepo, ILogger<NotificationController> logger)
        {
            _notificationRepo = notificationRepo;
            _logger = logger;
        }

        private string? GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            try
            {
                var count = await _notificationRepo.GetUnreadCountAsync(userId);
                return Json(new { success = true, count });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Notification] Error getting unread count for {UserId}", userId);
                return Json(new { success = false, count = 0 });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetMyNotifications()
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            try
            {
                var notifications = await _notificationRepo.GetNotificationsByUserIdAsync(userId);
                var mappedNotifications = notifications.Select(n => new
                {
                    id = n.Id,
                    userId = n.UserId,
                    title = n.Title,
                    message = n.Message,
                    type = n.Type,
                    isRead = n.IsRead,
                    linkUrl = n.LinkUrl,
                    createdAt = n.CreatedAt
                });
                return Json(new { success = true, notifications = mappedNotifications });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Notification] Error getting notifications for {UserId}", userId);
                return Json(new { success = false, notifications = new List<object>() });
            }
        }

        [HttpPost]
        public async Task<IActionResult> MarkAsRead(string id)
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            try
            {
                await _notificationRepo.MarkAsReadAsync(id);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Notification] Error marking as read {NotificationId}", id);
                return Json(new { success = false });
            }
        }

        [HttpPost]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = GetCurrentUserId();
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            try
            {
                await _notificationRepo.MarkAllAsReadAsync(userId);
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Notification] Error marking all as read for {UserId}", userId);
                return Json(new { success = false });
            }
        }
    }
}
