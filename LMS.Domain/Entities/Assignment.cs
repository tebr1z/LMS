using LMS.Domain.Enums;

namespace LMS.Domain.Entities;

public class Assignment : BaseEntity
{
    public int CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public AssignmentType Type { get; set; } = AssignmentType.ReadingMaterial;
    public DateTime Deadline { get; set; }
    public int CreatedBy { get; set; }

    // Navigation properties
    public virtual Course Course { get; set; } = null!;
    public virtual ICollection<AssignmentSubmission> Submissions { get; set; } = new List<AssignmentSubmission>();
}

