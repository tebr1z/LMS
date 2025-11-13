namespace LMS.Domain.Entities;

public class StudentFlag : BaseEntity
{
    public int StudentId { get; set; }
    public int CreatedById { get; set; } // StudentOffice user who created the flag
    public string Reason { get; set; } = string.Empty; // Reason for flagging (e.g., "Low attendance", "Missing assignments")
    public string RecommendedAction { get; set; } = string.Empty; // Suggested intervention (e.g., "Contact student", "Schedule meeting")
    public bool IsResolved { get; set; } = false; // Whether the flag has been addressed
    public DateTime? ResolvedAt { get; set; } // When the flag was resolved
    public int? ResolvedById { get; set; } // User who resolved the flag (Teacher/Admin)
    public string? ResolutionNotes { get; set; } // Notes about how the flag was resolved

    // Navigation properties
}

