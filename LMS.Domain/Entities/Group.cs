namespace LMS.Domain.Entities;

public class Group : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int CreatedBy { get; set; }

    // Navigation properties
    public virtual ICollection<CourseGroup> CourseGroups { get; set; } = new List<CourseGroup>();
    public virtual ICollection<GroupUser> GroupUsers { get; set; } = new List<GroupUser>();
}

