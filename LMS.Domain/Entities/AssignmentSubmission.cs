namespace LMS.Domain.Entities;

public class AssignmentSubmission : BaseEntity
{
    public int AssignmentId { get; set; }
    public int StudentId { get; set; }
    public string? FileUrl { get; set; }
    public string? AnswerText { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public decimal? Score { get; set; }
    public int? EvaluatedBy { get; set; }

    // Navigation properties
    public virtual Assignment Assignment { get; set; } = null!;
}

