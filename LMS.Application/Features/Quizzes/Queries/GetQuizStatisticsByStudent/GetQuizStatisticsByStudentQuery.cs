using MediatR;

namespace LMS.Application.Features.Quizzes.Queries.GetQuizStatisticsByStudent;

public class GetQuizStatisticsByStudentQuery : IRequest<QuizStatisticsDto>
{
    public int StudentId { get; set; }
    public int? QuizId { get; set; } // Optional: filter by specific quiz
    public int? AssignmentId { get; set; } // Optional: filter by assignment
}

public class QuizStatisticsDto
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public List<QuizSessionStatisticsDto> Sessions { get; set; } = new();
    public QuizOverallStatisticsDto Overall { get; set; } = new();
}

public class QuizSessionStatisticsDto
{
    public int SessionId { get; set; }
    public int QuizId { get; set; }
    public string QuizName { get; set; } = string.Empty;
    public int AssignmentId { get; set; }
    public string AssignmentTitle { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public bool IsCompleted { get; set; }
    public int TotalPoints { get; set; }
    public int PointsAwarded { get; set; }
    public decimal PercentageScore { get; set; }
    public int? Score { get; set; } // Mapped to Assignment.MaxScore
    public int? MaxScore { get; set; }
    public bool? Passed { get; set; }
    public int QuestionsAnswered { get; set; }
    public int TotalQuestions { get; set; }
}

public class QuizOverallStatisticsDto
{
    public int TotalQuizzesTaken { get; set; }
    public int CompletedQuizzes { get; set; }
    public int PassedQuizzes { get; set; }
    public decimal AveragePercentageScore { get; set; }
    public decimal AverageScore { get; set; }
}

