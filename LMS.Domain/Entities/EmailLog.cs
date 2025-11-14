namespace LMS.Domain.Entities;

/// <summary>
/// Email log entity for tracking all sent emails
/// </summary>
public class EmailLog : BaseEntity
{
    public string To { get; set; } = string.Empty; // Recipient email address
    public string Subject { get; set; } = string.Empty; // Email subject
    public string? Body { get; set; } // Email body (optional, might be large)
    public DateTime SentAt { get; set; } = DateTime.UtcNow; // When email was sent
    public string Status { get; set; } = "Sent"; // "Sent", "Failed", "Pending"
    public string? Error { get; set; } // Error message if sending failed
    public string? TemplateType { get; set; } // Type of email template used
    public int? UserId { get; set; } // Related user ID (optional)
    public int? RelatedEntityId { get; set; } // Related entity ID (e.g., assignment ID, submission ID)
    public string? RelatedEntityType { get; set; } // Related entity type (e.g., "Assignment", "Submission")
}


