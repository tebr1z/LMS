namespace LMS.Application.Interfaces.Notifications;

/// <summary>
/// Interface for real-time notification service (SignalR, WebSockets, etc.)
/// </summary>
public interface IRealTimeNotificationService
{
    /// <summary>
    /// Sends a notification to a specific user
    /// </summary>
    /// <param name="userId">Target user ID</param>
    /// <param name="notification">Notification data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendToUserAsync(int userId, NotificationMessage notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification to multiple users
    /// </summary>
    /// <param name="userIds">List of target user IDs</param>
    /// <param name="notification">Notification data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendToUsersAsync(IEnumerable<int> userIds, NotificationMessage notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification to all connected users
    /// </summary>
    /// <param name="notification">Notification data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendToAllAsync(NotificationMessage notification, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification to users in a specific group/role
    /// </summary>
    /// <param name="groupName">Group or role name</param>
    /// <param name="notification">Notification data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendToGroupAsync(string groupName, NotificationMessage notification, CancellationToken cancellationToken = default);
}

/// <summary>
/// Notification message model
/// </summary>
public class NotificationMessage
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "info"; // info, success, warning, error
    public Dictionary<string, object>? Data { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

