namespace LMS.Domain.Entities;

public class AssignmentSubmission : BaseEntity
{
    public int AssignmentId { get; set; }
    public int StudentId { get; set; }
    public string? FileUrl { get; set; }
    public string? AnswerText { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public int? Score { get; set; }
    public string? Feedback { get; set; }
    public int? EvaluatedById { get; set; }
    public DateTime? EvaluatedAt { get; set; }
    public int? TimeOnPageInSeconds { get; set; } // Time student spent on assignment page (aggregate)
    public bool? Passed { get; set; } // Whether assignment was passed (based on threshold)
    public bool? IsExcellent { get; set; } // Whether assignment score is above high threshold (excellent)
    public decimal PercentageScore { get; set; } = 0; // Percentage score (0-100): (Score / MaxScore) * 100

    // Navigation properties
    public virtual Assignment Assignment { get; set; } = null!;
    public virtual ICollection<QuizSession> QuizSessions { get; set; } = new List<QuizSession>(); // Links to quiz sessions if this submission came from a quiz
}

