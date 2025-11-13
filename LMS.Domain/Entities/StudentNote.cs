namespace LMS.Domain.Entities;

public class StudentNote : BaseEntity
{
    public int StudentId { get; set; } // Student the note is about
    public int GroupId { get; set; } // Group context
    public int CreatedById { get; set; } // User who created the note (Mentor, Teacher, StudentOffice)
    public string Title { get; set; } = string.Empty; // Note title
    public string Content { get; set; } = string.Empty; // Note content
    public bool IsPrivate { get; set; } = true; // Private to StudentOffice & assigned Teachers (default: true)
    public bool IsImportant { get; set; } = false; // Mark as important

    // Navigation properties
    public virtual Group Group { get; set; } = null!;
}

