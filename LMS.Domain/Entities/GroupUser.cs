namespace LMS.Domain.Entities;

public class GroupUser : BaseEntity
{
    public int GroupId { get; set; }
    public int UserId { get; set; }
    public string Role { get; set; } = string.Empty; // e.g., "Leader", "Member", "Moderator"

    // Navigation properties
    public virtual Group Group { get; set; } = null!;
}

