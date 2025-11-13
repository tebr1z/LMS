namespace LMS.Domain.Entities;

/// <summary>
/// Audit log entity for tracking changes to entities
/// </summary>
public class AuditLog : BaseEntity
{
    public string Entity { get; set; } = string.Empty; // e.g., "Assignment", "Submission", "CourseInstance"
    public int EntityId { get; set; } // ID of the entity being audited
    public string Action { get; set; } = string.Empty; // e.g., "Create", "Update", "Delete", "Grade"
    public int UserId { get; set; } // User who performed the action
    public string? OldValue { get; set; } // JSON string of old values (for updates)
    public string? NewValue { get; set; } // JSON string of new values
    public DateTime Timestamp { get; set; } = DateTime.UtcNow; // When the action occurred
    public string? Description { get; set; } // Optional description of the change
}

