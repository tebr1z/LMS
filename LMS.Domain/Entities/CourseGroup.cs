namespace LMS.Domain.Entities;

public class CourseGroup : BaseEntity
{
    public int CoursePreparedId { get; set; }
    public int GroupId { get; set; }

    // Navigation properties
    public virtual CoursePrepared CoursePrepared { get; set; } = null!;
    public virtual Group Group { get; set; } = null!;
}

