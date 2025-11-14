using LMS.Application.Interfaces;
using LMS.Application.Interfaces.Notifications;
using LMS.Domain.Entities;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LMS.Infrastructure.Services.Notifications;

/// <summary>
/// SMTP Email service implementation using MailKit
/// </summary>
public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public EmailService(
        IConfiguration configuration,
        ILogger<EmailService> logger,
        IUnitOfWork unitOfWork)
    {
        _configuration = configuration;
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> SendEmailAsync(
        string to,
        string subject,
        string body,
        bool isHtml = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var emailHost = _configuration["Email:Host"];
            var emailPort = _configuration.GetValue<int>("Email:Port", 587);
            var emailUser = _configuration["Email:User"];
            var emailPassword = _configuration["Email:Password"];
            var emailFrom = _configuration["Email:From"];
            var enableSsl = _configuration.GetValue<bool>("Email:EnableSsl", true);

            if (string.IsNullOrEmpty(emailHost) || string.IsNullOrEmpty(emailUser) || 
                string.IsNullOrEmpty(emailPassword) || string.IsNullOrEmpty(emailFrom))
            {
                _logger.LogWarning("Email configuration is incomplete. Email not sent to {To}", to);
                
                // Log as failed
                await LogEmailAsync(to, subject, body, "Failed", "Email configuration is incomplete", null, null, null, cancellationToken);
                return false;
            }

            // Create email message
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(emailFrom));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder();
            if (isHtml)
            {
                bodyBuilder.HtmlBody = body;
            }
            else
            {
                bodyBuilder.TextBody = body;
            }
            message.Body = bodyBuilder.ToMessageBody();

            // Send email via SMTP
            using var client = new SmtpClient();
            await client.ConnectAsync(emailHost, emailPort, enableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None, cancellationToken);
            await client.AuthenticateAsync(emailUser, emailPassword, cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("Email sent successfully to {To} with subject: {Subject}", to, subject);
            
            // Log as sent
            await LogEmailAsync(to, subject, body, "Sent", null, null, null, null, null, cancellationToken);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {To}", to);
            
            // Log as failed
            await LogEmailAsync(to, subject, body, "Failed", ex.Message, null, null, null, null, cancellationToken);
            
            return false;
        }
    }

    public async Task<bool> SendBulkEmailAsync(
        IEnumerable<string> to,
        string subject,
        string body,
        bool isHtml = true,
        CancellationToken cancellationToken = default)
    {
        var results = new List<bool>();
        foreach (var recipient in to)
        {
            var result = await SendEmailAsync(recipient, subject, body, isHtml, cancellationToken);
            results.Add(result);
        }
        return results.All(r => r);
    }

    public async Task<bool> SendEmailWithAttachmentsAsync(
        string to,
        string subject,
        string body,
        IEnumerable<EmailAttachment> attachments,
        bool isHtml = true,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // TODO: Implement email with attachments
            var emailHost = _configuration["Email:Host"];
            var emailPort = _configuration.GetValue<int>("Email:Port", 587);
            var emailUser = _configuration["Email:User"];
            var emailPassword = _configuration["Email:Password"];
            var emailFrom = _configuration["Email:From"];
            var enableSsl = _configuration.GetValue<bool>("Email:EnableSsl", true);

            if (string.IsNullOrEmpty(emailHost) || string.IsNullOrEmpty(emailUser) || 
                string.IsNullOrEmpty(emailPassword) || string.IsNullOrEmpty(emailFrom))
            {
                _logger.LogWarning("Email configuration is incomplete. Email not sent to {To}", to);
                await LogEmailAsync(to, subject, body, "Failed", "Email configuration is incomplete", null, null, null, null, cancellationToken);
                return false;
            }

            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(emailFrom));
            message.To.Add(MailboxAddress.Parse(to));
            message.Subject = subject;

            var bodyBuilder = new BodyBuilder();
            if (isHtml)
            {
                bodyBuilder.HtmlBody = body;
            }
            else
            {
                bodyBuilder.TextBody = body;
            }

            foreach (var attachment in attachments)
            {
                bodyBuilder.Attachments.Add(attachment.FileName, attachment.ContentStream, ContentType.Parse(attachment.ContentType));
            }

            message.Body = bodyBuilder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(emailHost, emailPort, enableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None, cancellationToken);
            await client.AuthenticateAsync(emailUser, emailPassword, cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);

            _logger.LogInformation("Email with attachments sent successfully to {To}", to);
            await LogEmailAsync(to, subject, body, "Sent", null, null, null, null, null, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email with attachments to {To}", to);
            await LogEmailAsync(to, subject, body, "Failed", ex.Message, null, null, null, null, cancellationToken);
            return false;
        }
    }

    private async Task LogEmailAsync(
        string to,
        string subject,
        string? body,
        string status,
        string? error,
        string? templateType,
        int? userId,
        int? relatedEntityId,
        string? relatedEntityType,
        CancellationToken cancellationToken)
    {
        try
        {
            var truncatedBody = !string.IsNullOrEmpty(body) && body.Length > 5000
                ? body.Substring(0, 5000) + "... [truncated]"
                : body;

            var emailLog = new EmailLog
            {
                To = to,
                Subject = subject,
                Body = truncatedBody,
                SentAt = DateTime.UtcNow,
                Status = status,
                Error = error,
                TemplateType = templateType,
                UserId = userId,
                RelatedEntityId = relatedEntityId,
                RelatedEntityType = relatedEntityType,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.EmailLogs.AddAsync(emailLog);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging email to {To}", to);
        }
    }
}

