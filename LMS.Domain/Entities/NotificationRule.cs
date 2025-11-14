using LMS.Domain.Enums;

namespace LMS.Domain.Entities;

/// <summary>
/// Represents a notification rule for smart predictive alerts
/// </summary>
public class NotificationRule : BaseEntity
{
    public string Name { get; set; } = string.Empty; // Rule name/description
    public string ConditionJson { get; set; } = string.Empty; // JSON string defining the condition (e.g., {"type": "inactive_days", "threshold": 3})
    public UserRole TargetRole { get; set; } // Role to target (e.g., Student, Teacher)
    public string MessageTemplate { get; set; } = string.Empty; // Message template with placeholders (e.g., "You have been inactive for {days} days")
    public bool IsActive { get; set; } = true; // Whether the rule is active
    public string? Type { get; set; } // Notification type (e.g., "inactivity", "deadline_approaching", "low_performance")
    public int? Priority { get; set; } // Priority level (1-10, higher = more important)
}


