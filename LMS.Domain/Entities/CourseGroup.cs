namespace LMS.Domain.Entities;

public class CourseGroup : BaseEntity
{
    public int CourseId { get; set; }
    public int GroupId { get; set; }

    // Navigation properties
    public virtual Course Course { get; set; } = null!;
    public virtual Group Group { get; set; } = null!;
}

