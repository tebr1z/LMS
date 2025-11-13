namespace LMS.Domain.Entities;

/// <summary>
/// AI-generated feedback for assignment submissions
/// </summary>
public class AssignmentFeedbackAI : BaseEntity
{
    public int SubmissionId { get; set; }
    public string AIComment { get; set; } = string.Empty;
    public decimal? ConfidenceScore { get; set; } // 0-1 scale indicating AI confidence
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public virtual AssignmentSubmission Submission { get; set; } = null!;
}

