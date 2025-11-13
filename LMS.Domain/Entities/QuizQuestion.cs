namespace LMS.Domain.Entities;

public class QuizQuestion : BaseEntity
{
    public int QuizId { get; set; }
    public string Text { get; set; } = string.Empty; // Question text
    public string Options { get; set; } = string.Empty; // JSON array of options: ["Option 1", "Option 2", ...]
    public string CorrectAnswer { get; set; } = string.Empty; // Can be single answer or comma-separated for multiple correct answers
    public int Points { get; set; } = 1; // Points awarded for correct answer
    public int Order { get; set; } = 0; // Order of question in quiz

    // Navigation properties
    public virtual Quiz Quiz { get; set; } = null!;
    public virtual ICollection<QuizResponse> Responses { get; set; } = new List<QuizResponse>();
}

