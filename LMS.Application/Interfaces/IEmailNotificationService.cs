namespace LMS.Application.Interfaces;

/// <summary>
/// Service interface for sending templated email notifications
/// </summary>
public interface IEmailNotificationService
{
    Task SendWelcomeEmailAsync(int userId, string email, string studentName, string loginUrl, CancellationToken cancellationToken = default);
    Task SendNewAssignmentEmailAsync(int assignmentId, int courseInstanceId, CancellationToken cancellationToken = default);
    Task SendDeadlineReminderEmailAsync(int assignmentId, int studentId, int hoursUntilDeadline, CancellationToken cancellationToken = default);
    Task SendGradePostedEmailAsync(int submissionId, CancellationToken cancellationToken = default);
    Task SendPaymentReminderEmailAsync(int paymentId, int userId, CancellationToken cancellationToken = default);
    Task SendSecurityAlertEmailAsync(int userId, string email, string studentName, int failedAttempts, string resetPasswordUrl, CancellationToken cancellationToken = default);
    Task SendEmailVerificationAsync(int userId, string email, string studentName, string verificationUrl, CancellationToken cancellationToken = default);
}

