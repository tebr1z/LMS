namespace LMS.Domain.Entities;

public class QuizSession : BaseEntity
{
    public int StudentId { get; set; }
    public int QuizId { get; set; }
    public int AssignmentId { get; set; } // For quick access
    public DateTime StartedAt { get; set; } = DateTime.UtcNow; // When student started the quiz
    public DateTime? EndedAt { get; set; } // When student ended/submitted the quiz
    public DateTime? ExpiresAt { get; set; } // When the quiz session expires (based on TimeLimitSeconds)
    public bool IsCompleted { get; set; } = false; // Whether quiz is completed
    public int TotalPoints { get; set; } = 0; // Total points possible
    public int PointsAwarded { get; set; } = 0; // Total points awarded
    public int? Score { get; set; } // Final score mapped to Assignment.MaxScore (0..MaxScore)
    public decimal PercentageScore { get; set; } = 0; // Percentage score (0-100)
    public bool? Passed { get; set; } // Whether student passed (based on PassingThreshold)
    public int? SubmissionId { get; set; } // Links to AssignmentSubmission if quiz was completed

    // Navigation properties
    public virtual Quiz Quiz { get; set; } = null!;
    public virtual Assignment Assignment { get; set; } = null!;
    public virtual ICollection<QuizResponse> Responses { get; set; } = new List<QuizResponse>();
    public virtual AssignmentSubmission? Submission { get; set; }
}

