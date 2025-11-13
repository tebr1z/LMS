namespace LMS.Domain.Entities;

public class Attendance : BaseEntity
{
    public int GroupId { get; set; }
    public int StudentId { get; set; }
    public DateTime Date { get; set; } // Date of attendance
    public bool Present { get; set; } // true = present, false = absent
    public int MarkedById { get; set; } // User who marked attendance (Mentor, Teacher, or StudentOffice)
    public string? Notes { get; set; } // Optional notes about attendance

    // Navigation properties
    public virtual Group Group { get; set; } = null!;
}

