namespace LMS.Domain.Entities;

public class Quiz : BaseEntity
{
    public int AssignmentId { get; set; } // Links to Assignment (AssignmentType = Test)
    public string QuizId { get; set; } = string.Empty; // Unique identifier for the quiz (can be same as AssignmentId or GUID)
    public int? TimeLimitSeconds { get; set; } // Nullable time limit in seconds
    public bool ShuffleQuestions { get; set; } = false; // Whether to shuffle questions for each student
    public int? PassingThreshold { get; set; } // Passing percentage (e.g., 80 for 80%)

    // Navigation properties
    public virtual Assignment Assignment { get; set; } = null!;
    public virtual ICollection<QuizQuestion> Questions { get; set; } = new List<QuizQuestion>();
    public virtual ICollection<QuizSession> Sessions { get; set; } = new List<QuizSession>();
}

