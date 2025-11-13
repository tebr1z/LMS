using MediatR;

namespace LMS.Application.Features.Quizzes.Queries.GetQuizStatisticsByGroup;

public class GetQuizStatisticsByGroupQuery : IRequest<GroupQuizStatisticsDto>
{
    public int GroupId { get; set; }
    public int? QuizId { get; set; } // Optional: filter by specific quiz
    public int? AssignmentId { get; set; } // Optional: filter by assignment
}

public class GroupQuizStatisticsDto
{
    public int GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public List<StudentQuizStatisticsDto> Students { get; set; } = new();
    public GroupOverallStatisticsDto Overall { get; set; } = new();
}

public class StudentQuizStatisticsDto
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public int SessionsCount { get; set; }
    public int CompletedSessionsCount { get; set; }
    public int PassedSessionsCount { get; set; }
    public decimal AveragePercentageScore { get; set; }
    public decimal AverageScore { get; set; }
    public List<GroupQuizSessionStatisticsDto> Sessions { get; set; } = new();
}

public class GroupQuizSessionStatisticsDto
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
    public int? Score { get; set; }
    public int? MaxScore { get; set; }
    public bool? Passed { get; set; }
    public int QuestionsAnswered { get; set; }
    public int TotalQuestions { get; set; }
}

public class GroupOverallStatisticsDto
{
    public int TotalStudents { get; set; }
    public int StudentsWhoTookQuiz { get; set; }
    public int StudentsWhoCompleted { get; set; }
    public int StudentsWhoPassed { get; set; }
    public decimal AveragePercentageScore { get; set; }
    public decimal AverageScore { get; set; }
}

