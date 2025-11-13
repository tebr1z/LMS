using LMS.Domain.Entities;

namespace LMS.Application.Interfaces;

public interface INotificationRepository : IRepository<Notification>
{
    Task<List<Notification>> GetByUserIdAsync(int userId, bool? isRead = null);
    Task<int> GetUnreadCountByUserIdAsync(int userId);
    Task<List<Notification>> GetRecentByUserIdAsync(int userId, int count = 20);
    Task MarkAsReadAsync(int notificationId, int userId);
    Task MarkAllAsReadAsync(int userId);
}

