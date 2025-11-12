namespace LMS.Application.Interfaces.Notifications;

/// <summary>
/// Interface for email notification service
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends an email
    /// </summary>
    /// <param name="to">Recipient email address</param>
    /// <param name="subject">Email subject</param>
    /// <param name="body">Email body (HTML or plain text)</param>
    /// <param name="isHtml">Whether the body is HTML</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if email was sent successfully</returns>
    Task<bool> SendEmailAsync(
        string to,
        string subject,
        string body,
        bool isHtml = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an email to multiple recipients
    /// </summary>
    /// <param name="to">List of recipient email addresses</param>
    /// <param name="subject">Email subject</param>
    /// <param name="body">Email body (HTML or plain text)</param>
    /// <param name="isHtml">Whether the body is HTML</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if all emails were sent successfully</returns>
    Task<bool> SendBulkEmailAsync(
        IEnumerable<string> to,
        string subject,
        string body,
        bool isHtml = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends an email with attachments
    /// </summary>
    /// <param name="to">Recipient email address</param>
    /// <param name="subject">Email subject</param>
    /// <param name="body">Email body</param>
    /// <param name="attachments">List of file attachments (file path or stream)</param>
    /// <param name="isHtml">Whether the body is HTML</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if email was sent successfully</returns>
    Task<bool> SendEmailWithAttachmentsAsync(
        string to,
        string subject,
        string body,
        IEnumerable<EmailAttachment> attachments,
        bool isHtml = true,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Email attachment model
/// </summary>
public class EmailAttachment
{
    public string FileName { get; set; } = string.Empty;
    public Stream ContentStream { get; set; } = null!;
    public string ContentType { get; set; } = "application/octet-stream";
}

