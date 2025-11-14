namespace LMS.Application.Interfaces;

/// <summary>
/// Service interface for email template rendering
/// </summary>
public interface IEmailTemplateService
{
    /// <summary>
    /// Renders an email template with provided data
    /// </summary>
    Task<(string Subject, string HtmlBody)> RenderTemplateAsync(
        string templateType,
        Dictionary<string, string> data,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets template HTML from file if exists, otherwise returns template body from database
    /// </summary>
    Task<string> GetTemplateHtmlAsync(
        string templateType,
        Dictionary<string, string> data,
        CancellationToken cancellationToken = default);
}


