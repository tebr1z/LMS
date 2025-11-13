using LMS.Domain.Enums;

namespace LMS.Domain.Entities;

public class Assignment : BaseEntity
{
    public int? CoursePreparedId { get; set; } // Nullable: template assignment in CoursePrepared
    public int? CourseId { get; set; } // Nullable: can be linked to Course
    public int? CourseInstanceId { get; set; } // Nullable: instance assignment in CourseInstance
    public int? GroupId { get; set; } // Nullable: assignment can be course-wide or group-specific
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public AssignmentType AssignmentType { get; set; } = AssignmentType.ReadingMaterial;
    public int MaxScore { get; set; } = 100;
    public int CreatedById { get; set; }
    public bool IsPublished { get; set; } = false; // Whether assignment is visible to students
    public bool AllowEditAfterPublish { get; set; }
    public bool AllowResubmit { get; set; }
    public DateTime? Deadline { get; set; } // Nullable deadline

    // Navigation properties
    public virtual CoursePrepared? CoursePrepared { get; set; }
    public virtual Course? Course { get; set; }
    public virtual CourseInstance? CourseInstance { get; set; }
    public virtual Group? Group { get; set; }
    public virtual ICollection<AssignmentSubmission> Submissions { get; set; } = new List<AssignmentSubmission>();
}

