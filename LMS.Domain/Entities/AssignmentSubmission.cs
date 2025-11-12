namespace LMS.Domain.Entities;

public class AssignmentSubmission : BaseEntity
{
    public int AssignmentId { get; set; }
    public int StudentId { get; set; }
    public string? FileUrl { get; set; }
    public string? AnswerText { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public int? Score { get; set; }
    public int? EvaluatedById { get; set; }
    public DateTime? EvaluatedAt { get; set; }
    public int? TimeOnPageInSeconds { get; set; } // Time student spent on assignment page (aggregate)

    // Navigation properties
    public virtual Assignment Assignment { get; set; } = null!;
}

