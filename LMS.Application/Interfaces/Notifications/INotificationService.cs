namespace LMS.Application.Interfaces.Notifications;

/// <summary>
/// Unified notification service that combines email and real-time notifications
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Sends a notification via email and real-time channels
    /// </summary>
    /// <param name="userId">Target user ID</param>
    /// <param name="email">User email address</param>
    /// <param name="notification">Notification data</param>
    /// <param name="sendEmail">Whether to send email</param>
    /// <param name="sendRealTime">Whether to send real-time notification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendNotificationAsync(
        int userId,
        string email,
        NotificationMessage notification,
        bool sendEmail = true,
        bool sendRealTime = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a course enrollment notification
    /// </summary>
    Task SendEnrollmentNotificationAsync(int userId, string email, int courseId, string courseTitle, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a course creation notification
    /// </summary>
    Task SendCourseCreatedNotificationAsync(int userId, string email, int courseId, string courseTitle, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a message notification
    /// </summary>
    Task SendMessageNotificationAsync(int userId, string email, int senderId, string senderName, string message, CancellationToken cancellationToken = default);
}

