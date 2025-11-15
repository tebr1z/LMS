using LMS.Application.Interfaces;
using LMS.Application.Interfaces.Notifications;
using LMS.Domain.Entities;
using LMS.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LMS.Infrastructure.Services;

/// <summary>
/// Service for sending templated email notifications
/// </summary>
public class EmailNotificationService : IEmailNotificationService
{
    private readonly IEmailService _emailService;
    private readonly IEmailTemplateService _emailTemplateService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailNotificationService> _logger;

    public EmailNotificationService(
        IEmailService emailService,
        IEmailTemplateService emailTemplateService,
        IUnitOfWork unitOfWork,
        IUserRepository userRepository,
        IConfiguration configuration,
        ILogger<EmailNotificationService> logger)
    {
        _emailService = emailService;
        _emailTemplateService = emailTemplateService;
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendWelcomeEmailAsync(int userId, string email, string studentName, string loginUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            var data = new Dictionary<string, string>
            {
                { "StudentName", studentName },
                { "LoginUrl", loginUrl },
                { "Year", DateTime.UtcNow.Year.ToString() }
            };

            var (subject, htmlBody) = await _emailTemplateService.RenderTemplateAsync("Welcome", data, cancellationToken);

            var sent = await _emailService.SendEmailAsync(email, subject, htmlBody, true, cancellationToken);
            
            if (sent)
            {
                _logger.LogInformation("Welcome email sent to {Email} for user {UserId}", email, userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending welcome email to {Email}", email);
        }
    }

    public async Task SendNewAssignmentEmailAsync(int assignmentId, int courseInstanceId, CancellationToken cancellationToken = default)
    {
        try
        {
            var assignment = await _unitOfWork.Assignments.GetByIdAsync(assignmentId);
            if (assignment == null || !assignment.IsPublished)
                return;

            var courseInstance = await _unitOfWork.CourseInstances.GetByIdAsync(courseInstanceId);
            if (courseInstance == null)
                return;

            var teacher = await _userRepository.GetUserByIdAsync(assignment.CreatedById);
            var teacherName = teacher?.FullName ?? "Teacher";

            // Get all students in the group
            var groupUsers = await _unitOfWork.GroupUsers.GetGroupUsersByGroupIdAsync(courseInstance.GroupId);
            var studentIds = groupUsers
                .Where(gu => gu.Role == GroupRole.Student)
                .Select(gu => gu.UserId)
                .ToList();

            if (!studentIds.Any())
                return;

            var students = await _userRepository.GetUsersByIdsAsync(studentIds);
            var baseUrl = _configuration["AppBaseUrl"] ?? "https://yourlms.com";
            var assignmentUrl = $"{baseUrl}/assignments/{assignmentId}";

            var data = new Dictionary<string, string>
            {
                { "AssignmentTitle", assignment.Title },
                { "TeacherName", teacherName },
                { "Deadline", assignment.Deadline?.ToString("yyyy-MM-dd HH:mm") ?? "No deadline" },
                { "MaxScore", assignment.MaxScore.ToString() },
                { "Description", assignment.Description ?? "" },
                { "AssignmentUrl", assignmentUrl },
                { "Year", DateTime.UtcNow.Year.ToString() }
            };

            var (subject, htmlBody) = await _emailTemplateService.RenderTemplateAsync("NewAssignment", data, cancellationToken);

            // Send to all students
            var studentsList = students.Where(s => !string.IsNullOrEmpty(s.Email)).ToList();
            foreach (var student in studentsList)
            {
                var studentData = new Dictionary<string, string>(data)
                {
                    { "StudentName", student.FullName }
                };

                var (studentSubject, studentBody) = await _emailTemplateService.RenderTemplateAsync("NewAssignment", studentData, cancellationToken);

                await _emailService.SendEmailAsync(
                    student.Email!,
                    studentSubject,
                    studentBody,
                    true,
                    cancellationToken);
            }

            _logger.LogInformation(eventId: default, "New assignment emails sent for assignment {AssignmentId} to {Count} students", assignmentId, studentsList.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending new assignment emails for assignment {AssignmentId}", assignmentId);
        }
    }

    public async Task SendDeadlineReminderEmailAsync(int assignmentId, int studentId, int hoursUntilDeadline, CancellationToken cancellationToken = default)
    {
        try
        {
            var assignment = await _unitOfWork.Assignments.GetByIdAsync(assignmentId);
            if (assignment == null || !assignment.Deadline.HasValue)
                return;

            var student = await _userRepository.GetUserByIdAsync(studentId);
            if (student == null || string.IsNullOrEmpty(student.Email))
                return;

            var baseUrl = _configuration["AppBaseUrl"] ?? "https://yourlms.com";
            var assignmentUrl = $"{baseUrl}/assignments/{assignmentId}";

            var data = new Dictionary<string, string>
            {
                { "StudentName", student.FullName },
                { "AssignmentTitle", assignment.Title },
                { "Deadline", assignment.Deadline.Value.ToString("yyyy-MM-dd HH:mm") },
                { "HoursUntilDeadline", hoursUntilDeadline.ToString() },
                { "AssignmentUrl", assignmentUrl },
                { "Year", DateTime.UtcNow.Year.ToString() }
            };

            var (subject, htmlBody) = await _emailTemplateService.RenderTemplateAsync("DeadlineReminder", data, cancellationToken);

            await _emailService.SendEmailAsync(student.Email, subject, htmlBody, true, cancellationToken);
            
            _logger.LogInformation("Deadline reminder email sent to {Email} for assignment {AssignmentId}", student.Email, assignmentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending deadline reminder email to student {StudentId} for assignment {AssignmentId}", studentId, assignmentId);
        }
    }

    public async Task SendGradePostedEmailAsync(int submissionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var submission = await _unitOfWork.AssignmentSubmissions.GetByIdAsync(submissionId);
            if (submission == null || !submission.Score.HasValue)
                return;

            var assignment = await _unitOfWork.Assignments.GetByIdAsync(submission.AssignmentId);
            if (assignment == null)
                return;

            var student = await _userRepository.GetUserByIdAsync(submission.StudentId);
            if (student == null || string.IsNullOrEmpty(student.Email))
                return;

            var teacher = submission.EvaluatedById.HasValue
                ? await _userRepository.GetUserByIdAsync(submission.EvaluatedById.Value)
                : null;
            var teacherName = teacher?.FullName ?? "Teacher";

            var baseUrl = _configuration["AppBaseUrl"] ?? "https://yourlms.com";
            var assignmentUrl = $"{baseUrl}/assignments/{assignment.Id}";

            var data = new Dictionary<string, string>
            {
                { "StudentName", student.FullName },
                { "AssignmentTitle", assignment.Title },
                { "TeacherName", teacherName },
                { "Score", submission.Score.Value.ToString() },
                { "MaxScore", assignment.MaxScore.ToString() },
                { "PercentageScore", submission.PercentageScore.ToString("F1") },
                { "Feedback", submission.Feedback ?? "" },
                { "Passed", submission.Passed == true ? "true" : "false" },
                { "IsExcellent", submission.IsExcellent == true ? "true" : "false" },
                { "AssignmentUrl", assignmentUrl },
                { "Year", DateTime.UtcNow.Year.ToString() }
            };

            var (subject, htmlBody) = await _emailTemplateService.RenderTemplateAsync("GradePosted", data, cancellationToken);

            await _emailService.SendEmailAsync(student.Email, subject, htmlBody, true, cancellationToken);
            
            _logger.LogInformation("Grade posted email sent to {Email} for submission {SubmissionId}", student.Email, submissionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending grade posted email for submission {SubmissionId}", submissionId);
        }
    }

    public async Task SendPaymentReminderEmailAsync(int paymentId, int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var payment = await _unitOfWork.Payments.GetByIdAsync(paymentId);
            if (payment == null || payment.Status != PaymentStatus.Pending)
                return;

            var user = await _userRepository.GetUserByIdAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.Email))
                return;

            var baseUrl = _configuration["AppBaseUrl"] ?? "https://yourlms.com";
            var paymentUrl = $"{baseUrl}/payments/{paymentId}";

            var daysUntilDue = payment.DueDate.HasValue
                ? (int)Math.Ceiling((payment.DueDate.Value - DateTime.UtcNow).TotalDays)
                : 0;

            var data = new Dictionary<string, string>
            {
                { "StudentName", user.FullName },
                { "Amount", payment.Amount.ToString("F2") },
                { "Currency", "USD" }, // TODO: Get from settings
                { "DueDate", payment.DueDate?.ToString("yyyy-MM-dd") ?? "N/A" },
                { "DaysUntilDue", daysUntilDue.ToString() },
                { "PaymentUrl", paymentUrl },
                { "Year", DateTime.UtcNow.Year.ToString() }
            };

            var (subject, htmlBody) = await _emailTemplateService.RenderTemplateAsync("PaymentReminder", data, cancellationToken);

            await _emailService.SendEmailAsync(user.Email, subject, htmlBody, true, cancellationToken);
            
            _logger.LogInformation("Payment reminder email sent to {Email} for payment {PaymentId}", user.Email, paymentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending payment reminder email for payment {PaymentId}", paymentId);
        }
    }

    public async Task SendSecurityAlertEmailAsync(int userId, string email, string studentName, int failedAttempts, string resetPasswordUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            var data = new Dictionary<string, string>
            {
                { "StudentName", studentName },
                { "FailedAttempts", failedAttempts.ToString() },
                { "Timestamp", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss UTC") },
                { "ResetPasswordUrl", resetPasswordUrl },
                { "Year", DateTime.UtcNow.Year.ToString() }
            };

            var (subject, htmlBody) = await _emailTemplateService.RenderTemplateAsync("SecurityAlert", data, cancellationToken);

            await _emailService.SendEmailAsync(email, subject, htmlBody, true, cancellationToken);
            
            _logger.LogInformation("Security alert email sent to {Email} for user {UserId} after {FailedAttempts} failed attempts", email, userId, failedAttempts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending security alert email to {Email}", email);
        }
    }

    public async Task SendEmailVerificationAsync(int userId, string email, string studentName, string verificationUrl, CancellationToken cancellationToken = default)
    {
        try
        {
            var data = new Dictionary<string, string>
            {
                { "StudentName", studentName },
                { "VerificationUrl", verificationUrl },
                { "Year", DateTime.UtcNow.Year.ToString() }
            };

            var (subject, htmlBody) = await _emailTemplateService.RenderTemplateAsync("EmailVerification", data, cancellationToken);

            await _emailService.SendEmailAsync(email, subject, htmlBody, true, cancellationToken);
            
            _logger.LogInformation("Email verification email sent to {Email} for user {UserId}", email, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email verification email to {Email}", email);
        }
    }
}

