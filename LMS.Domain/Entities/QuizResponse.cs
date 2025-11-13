namespace LMS.Domain.Entities;

public class QuizResponse : BaseEntity
{
    public int StudentId { get; set; }
    public int QuizId { get; set; }
    public int QuizQuestionId { get; set; }
    public int QuizSessionId { get; set; } // Links to QuizSession
    public string SelectedOption { get; set; } = string.Empty; // Selected option(s) - can be comma-separated for multiple
    public string? AnswerText { get; set; } // Text answer for open-ended questions
    public bool IsCorrect { get; set; } = false; // Whether the answer is correct
    public int PointsAwarded { get; set; } = 0; // Points awarded for this response
    public DateTime AnsweredAt { get; set; } = DateTime.UtcNow; // When the answer was submitted

    // Navigation properties
    public virtual QuizQuestion Question { get; set; } = null!;
    public virtual QuizSession Session { get; set; } = null!;
}

