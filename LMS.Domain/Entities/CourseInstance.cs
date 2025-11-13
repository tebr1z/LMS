namespace LMS.Domain.Entities;

public class CourseInstance : BaseEntity
{
    public int CoursePreparedId { get; set; }
    public int GroupId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Content { get; set; } // Copy of CoursePrepared.DefaultContent

    // Navigation properties
    public virtual CoursePrepared CoursePrepared { get; set; } = null!;
    public virtual Group Group { get; set; } = null!;
    public virtual ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
}

