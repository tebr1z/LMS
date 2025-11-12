namespace LMS.Domain.Entities;

public class CoursePrepared : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? DefaultContent { get; set; } // Template content for the prepared course

    // Navigation properties
    public virtual ICollection<CourseGroup> CourseGroups { get; set; } = new List<CourseGroup>();
    public virtual ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
}

