using LMS.Domain.Enums;

namespace LMS.Domain.Entities;

public class Notification : BaseEntity
{
    public int UserId { get; set; } // User to notify
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? Data { get; set; } // JSON data for additional context
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; } // When notification was read
    public NotificationChannel Channel { get; set; } = NotificationChannel.InApp;
    public string? Type { get; set; } // e.g., "invoice_overdue", "assignment_deadline", "student_flag"

    // Navigation properties
}

