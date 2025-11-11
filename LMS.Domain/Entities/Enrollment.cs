namespace LMS.Domain.Entities;

public class Enrollment : BaseEntity
{
    public int UserId { get; set; }
    public int CourseId { get; set; }
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Course Course { get; set; } = null!;
}

