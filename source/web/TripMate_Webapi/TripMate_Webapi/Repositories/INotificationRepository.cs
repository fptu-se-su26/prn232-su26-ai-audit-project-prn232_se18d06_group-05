using TripMate_Webapi.Entities;

namespace TripMate_Webapi.Repositories
{
    public interface INotificationRepository
    {
        Task<NotificationEntity> CreateNotificationAsync(NotificationEntity notification);
        Task<List<NotificationEntity>> GetNotificationsByUserIdAsync(string userId, int limit = 20);
        Task<int> GetUnreadCountAsync(string userId);
        Task MarkAsReadAsync(string notificationId);
        Task MarkAllAsReadAsync(string userId);
    }
}
