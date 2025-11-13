using MediatR;

namespace LMS.Application.Features.Quizzes.Commands.CreateQuiz;

public class CreateQuizCommand : IRequest<int>
{
    public int AssignmentId { get; set; }
    public int? TimeLimitSeconds { get; set; } // Nullable time limit
    public bool ShuffleQuestions { get; set; } = false;
    public int? PassingThreshold { get; set; } // Passing percentage (e.g., 80 for 80%)
    public List<QuizQuestionDto> Questions { get; set; } = new();
    public int CreatedById { get; set; } // Teacher creating the quiz
}

public class QuizQuestionDto
{
    public string Text { get; set; } = string.Empty;
    public List<string> Options { get; set; } = new(); // JSON array will be created from this
    public string CorrectAnswer { get; set; } = string.Empty; // Can be comma-separated for multiple correct answers
    public int Points { get; set; } = 1;
    public int Order { get; set; } = 0;
}

