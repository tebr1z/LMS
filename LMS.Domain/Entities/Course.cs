namespace LMS.Domain.Entities;

public class Course : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int CreatedBy { get; set; }

    // Navigation properties - Using object to avoid dependency on Infrastructure
    public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public virtual ICollection<CourseGroup> CourseGroups { get; set; } = new List<CourseGroup>();
    public virtual ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
}

