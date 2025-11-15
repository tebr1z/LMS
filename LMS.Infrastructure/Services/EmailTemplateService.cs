using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Text;
using IOFile = System.IO.File;

namespace LMS.Infrastructure.Services;

/// <summary>
/// Service for rendering email templates
/// </summary>
public class EmailTemplateService : IEmailTemplateService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<EmailTemplateService> _logger;
    private readonly string _templateRootPath;

    public EmailTemplateService(
        IUnitOfWork unitOfWork,
        ILogger<EmailTemplateService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
        _templateRootPath = Path.Combine(AppContext.BaseDirectory, "wwwroot", "email-templates");
    }

    public async Task<(string Subject, string HtmlBody)> RenderTemplateAsync(
        string templateType,
        Dictionary<string, string> data,
        CancellationToken cancellationToken = default)
    {
        EmailTemplate? template = null;
        try
        {
            template = await _unitOfWork.EmailTemplates.GetByTemplateTypeAsync(templateType, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Unable to load email template {TemplateType} from database. Falling back to defaults.", templateType);
        }

        if (template == null)
        {
            _logger.LogWarning("Email template {TemplateType} not found in database. Using default.", templateType);
            
            // Fallback to default template
            var defaultSubject = GetDefaultSubject(templateType);
            var defaultBody = GetDefaultBody(templateType);
            return (RenderString(defaultSubject, data), RenderString(defaultBody, data));
        }

        // Try to get HTML from file first, then fallback to database template
        var htmlBody = await GetTemplateHtmlAsync(templateType, data, cancellationToken);
        if (string.IsNullOrEmpty(htmlBody))
        {
            htmlBody = RenderString(template.HtmlBody, data);
        }

        var subject = RenderString(template.Subject, data);

        return (subject, htmlBody);
    }

    public async Task<string> GetTemplateHtmlAsync(
        string templateType,
        Dictionary<string, string> data,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to read template from wwwroot/email-templates/{templateType}.html
            var templatePath = Path.Combine(_templateRootPath, $"{templateType}.html");

            if (IOFile.Exists(templatePath))
            {
                var templateContent = await IOFile.ReadAllTextAsync(templatePath, cancellationToken);
                return RenderString(templateContent, data);
            }

            // If file doesn't exist, return empty string to use database template
            return string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error reading email template file for {TemplateType}", templateType);
            return string.Empty;
        }
    }

    private string RenderString(string template, Dictionary<string, string> data)
    {
        if (string.IsNullOrEmpty(template))
            return string.Empty;

        var result = template;
        
        foreach (var kvp in data)
        {
            result = result.Replace($"{{{kvp.Key}}}", kvp.Value ?? string.Empty);
        }

        return result;
    }

    private string GetDefaultSubject(string templateType)
    {
        return templateType switch
        {
            "NewAssignment" => "New Assignment: {AssignmentTitle}",
            "DeadlineReminder" => "Assignment Deadline Reminder: {AssignmentTitle}",
            "GradePosted" => "Grade Posted: {AssignmentTitle}",
            "PaymentReminder" => "Payment Reminder",
            "Welcome" => "Welcome to LMS, {StudentName}!",
            "SecurityAlert" => "Security Alert: Failed Login Attempts",
            _ => "LMS Notification"
        };
    }

    private string GetDefaultBody(string templateType)
    {
        return templateType switch
        {
            "NewAssignment" => GetDefaultNewAssignmentBody(),
            "DeadlineReminder" => GetDefaultDeadlineReminderBody(),
            "GradePosted" => GetDefaultGradePostedBody(),
            "PaymentReminder" => GetDefaultPaymentReminderBody(),
            "Welcome" => GetDefaultWelcomeBody(),
            "SecurityAlert" => GetDefaultSecurityAlertBody(),
            "EmailVerification" => GetDefaultEmailVerificationBody(),
            _ => "<p>LMS Notification</p>"
        };
    }

    private string GetDefaultNewAssignmentBody()
    {
        return @"<html>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <h2 style='color: #2c3e50;'>New Assignment Published</h2>
        <p>Hello <strong>{StudentName}</strong>,</p>
        <p>A new assignment <strong>{AssignmentTitle}</strong> has been published by <strong>{TeacherName}</strong>.</p>
        <div style='background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 20px 0;'>
            <p><strong>Deadline:</strong> {Deadline}</p>
        </div>
        <a href='{AssignmentUrl}' style='display: inline-block; background-color: #007bff; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; margin: 20px 0;'>Open Assignment</a>
        <p style='margin-top: 30px; color: #666; font-size: 12px;'>LMS Team</p>
    </div>
</body>
</html>";
    }

    private string GetDefaultDeadlineReminderBody()
    {
        return @"<html>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <h2 style='color: #dc3545;'>Assignment Deadline Reminder</h2>
        <p>Hello <strong>{StudentName}</strong>,</p>
        <p>This is a reminder that your assignment <strong>{AssignmentTitle}</strong> is due in <strong>{HoursUntilDeadline} hours</strong>.</p>
        <div style='background-color: #fff3cd; padding: 15px; border-radius: 5px; border-left: 4px solid #ffc107; margin: 20px 0;'>
            <p><strong>Deadline:</strong> {Deadline}</p>
        </div>
        <a href='{AssignmentUrl}' style='display: inline-block; background-color: #ffc107; color: #333; padding: 12px 24px; text-decoration: none; border-radius: 5px; margin: 20px 0; font-weight: bold;'>Submit Assignment</a>
        <p style='margin-top: 30px; color: #666; font-size: 12px;'>LMS Team</p>
    </div>
</body>
</html>";
    }

    private string GetDefaultGradePostedBody()
    {
        return @"<html>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <h2 style='color: #28a745;'>Grade Posted</h2>
        <p>Hello <strong>{StudentName}</strong>,</p>
        <p>Your assignment <strong>{AssignmentTitle}</strong> has been graded.</p>
        <div style='background-color: #d4edda; padding: 15px; border-radius: 5px; margin: 20px 0;'>
            <p><strong>Your Score:</strong> {Score} / {MaxScore} ({PercentageScore}%)</p>
            {#if Feedback}<p><strong>Feedback:</strong> {Feedback}</p>{/if}
        </div>
        <a href='{AssignmentUrl}' style='display: inline-block; background-color: #28a745; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; margin: 20px 0;'>View Assignment</a>
        <p style='margin-top: 30px; color: #666; font-size: 12px;'>LMS Team</p>
    </div>
</body>
</html>";
    }

    private string GetDefaultPaymentReminderBody()
    {
        return @"<html>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <h2 style='color: #dc3545;'>Payment Reminder</h2>
        <p>Hello <strong>{StudentName}</strong>,</p>
        <p>This is a reminder that you have a payment due.</p>
        <div style='background-color: #f8d7da; padding: 15px; border-radius: 5px; margin: 20px 0;'>
            <p><strong>Amount:</strong> {Amount} {Currency}</p>
            <p><strong>Due Date:</strong> {DueDate}</p>
        </div>
        <a href='{PaymentUrl}' style='display: inline-block; background-color: #dc3545; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; margin: 20px 0;'>Make Payment</a>
        <p style='margin-top: 30px; color: #666; font-size: 12px;'>LMS Team</p>
    </div>
</body>
</html>";
    }

    private string GetDefaultWelcomeBody()
    {
        return @"<html>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <h1 style='color: #007bff; text-align: center;'>Welcome to LMS!</h1>
        <p>Hello <strong>{StudentName}</strong>,</p>
        <p>Welcome to our Learning Management System! We're excited to have you join us.</p>
        <div style='background-color: #e7f3ff; padding: 20px; border-radius: 5px; margin: 20px 0;'>
            <h3 style='color: #0056b3;'>Getting Started</h3>
            <ul>
                <li>Explore your courses</li>
                <li>Complete assignments on time</li>
                <li>Track your progress</li>
            </ul>
        </div>
        <a href='{LoginUrl}' style='display: inline-block; background-color: #007bff; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; margin: 20px 0;'>Login to Your Account</a>
        <p style='margin-top: 30px; color: #666; font-size: 12px;'>If you have any questions, please don't hesitate to contact us.</p>
        <p style='color: #666; font-size: 12px;'>LMS Team</p>
    </div>
</body>
</html>";
    }

    private string GetDefaultSecurityAlertBody()
    {
        return @"<html>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <h2 style='color: #dc3545;'>Security Alert</h2>
        <p>Hello <strong>{StudentName}</strong>,</p>
        <div style='background-color: #f8d7da; padding: 15px; border-radius: 5px; border-left: 4px solid #dc3545; margin: 20px 0;'>
            <p><strong>We detected {FailedAttempts} failed login attempts on your account.</strong></p>
            <p>If this was you, please ensure you're using the correct password.</p>
            <p>If this wasn't you, please secure your account immediately.</p>
        </div>
        <a href='{ResetPasswordUrl}' style='display: inline-block; background-color: #dc3545; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; margin: 20px 0;'>Reset Password</a>
        <p style='margin-top: 30px; color: #666; font-size: 12px;'>If you did not attempt to log in, please contact support immediately.</p>
        <p style='color: #666; font-size: 12px;'>LMS Security Team</p>
    </div>
</body>
</html>";
    }

    private string GetDefaultEmailVerificationBody()
    {
        return @"<html>
<body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
    <div style='max-width: 600px; margin: 0 auto; padding: 20px;'>
        <h2 style='color: #667eea;'>Email Doğrulama</h2>
        <p>Merhaba <strong>{StudentName}</strong>,</p>
        <p>LMS sistemine hoş geldiniz! Hesabınızı aktif etmek için email adresinizi doğrulamanız gerekmektedir.</p>
        <div style='background-color: #e7f3ff; padding: 15px; border-radius: 5px; margin: 20px 0;'>
            <p><strong>Email Adresinizi Doğrulayın</strong></p>
            <p>Email adresinizi doğrulamak için aşağıdaki linke tıklayın.</p>
        </div>
        <a href='{VerificationUrl}' style='display: inline-block; background-color: #667eea; color: white; padding: 12px 24px; text-decoration: none; border-radius: 5px; margin: 20px 0;'>Email Adresimi Doğrula</a>
        <p style='margin-top: 30px; color: #666; font-size: 12px;'>LMS Team</p>
    </div>
</body>
</html>";
    }
}

