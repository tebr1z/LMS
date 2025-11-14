namespace LMS.Domain.Entities;

/// <summary>
/// Email template entity for storing email templates
/// </summary>
public class EmailTemplate : BaseEntity
{
    public string TemplateType { get; set; } = string.Empty; // e.g., "NewAssignment", "DeadlineReminder", "GradePosted", "PaymentReminder", "Welcome", "SecurityAlert"
    public string Subject { get; set; } = string.Empty; // Email subject template (can contain placeholders like {AssignmentTitle})
    public string HtmlBody { get; set; } = string.Empty; // HTML email body template (can contain placeholders)
    public bool IsActive { get; set; } = true; // Whether the template is active
    public string? Description { get; set; } // Description of when this template is used
}


