using LMS.Domain.Enums;

namespace LMS.Domain.Entities;

public class GroupUser : BaseEntity
{
    public int GroupId { get; set; }
    public int UserId { get; set; }
    public GroupRole Role { get; set; } // Local group role

    // Navigation properties
    public virtual Group Group { get; set; } = null!;
}

