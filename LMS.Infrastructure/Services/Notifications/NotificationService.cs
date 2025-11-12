using LMS.Application.Interfaces.Notifications;
using Microsoft.Extensions.Logging;

namespace LMS.Infrastructure.Services.Notifications;

/// <summary>
/// Unified notification service that combines email and real-time notifications
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IEmailService _emailService;
    private readonly IRealTimeNotificationService _realTimeNotificationService;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IEmailService emailService,
        IRealTimeNotificationService realTimeNotificationService,
        ILogger<NotificationService> logger)
    {
        _emailService = emailService;
        _realTimeNotificationService = realTimeNotificationService;
        _logger = logger;
    }

    public async Task SendNotificationAsync(
        int userId,
        string email,
        NotificationMessage notification,
        bool sendEmail = true,
        bool sendRealTime = true,
        CancellationToken cancellationToken = default)
    {
        var tasks = new List<Task>();

        if (sendEmail)
        {
            tasks.Add(_emailService.SendEmailAsync(
                email,
                notification.Title,
                notification.Message,
                isHtml: true,
                cancellationToken));
        }

        if (sendRealTime)
        {
            tasks.Add(_realTimeNotificationService.SendToUserAsync(userId, notification, cancellationToken));
        }

        await Task.WhenAll(tasks);
        _logger.LogInformation("Notification sent to user {UserId} (Email: {SendEmail}, RealTime: {SendRealTime})",
            userId, sendEmail, sendRealTime);
    }

    public async Task SendEnrollmentNotificationAsync(
        int userId,
        string email,
        int courseId,
        string courseTitle,
        CancellationToken cancellationToken = default)
    {
        var notification = new NotificationMessage
        {
            Title = "Course Enrollment",
            Message = $"You have been successfully enrolled in the course: {courseTitle}",
            Type = "success",
            Data = new Dictionary<string, object> { { "courseId", courseId } }
        };

        await SendNotificationAsync(userId, email, notification, cancellationToken: cancellationToken);
    }

    public async Task SendCourseCreatedNotificationAsync(
        int userId,
        string email,
        int courseId,
        string courseTitle,
        CancellationToken cancellationToken = default)
    {
        var notification = new NotificationMessage
        {
            Title = "Course Created",
            Message = $"Your course '{courseTitle}' has been created successfully.",
            Type = "success",
            Data = new Dictionary<string, object> { { "courseId", courseId } }
        };

        await SendNotificationAsync(userId, email, notification, cancellationToken: cancellationToken);
    }

    public async Task SendMessageNotificationAsync(
        int userId,
        string email,
        int senderId,
        string senderName,
        string message,
        CancellationToken cancellationToken = default)
    {
        var notification = new NotificationMessage
        {
            Title = $"New message from {senderName}",
            Message = message,
            Type = "info",
            Data = new Dictionary<string, object> { { "senderId", senderId } }
        };

        await SendNotificationAsync(userId, email, notification, sendEmail: false, cancellationToken: cancellationToken);
    }
}

