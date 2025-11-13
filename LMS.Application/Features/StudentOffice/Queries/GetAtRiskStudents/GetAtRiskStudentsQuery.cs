using MediatR;

namespace LMS.Application.Features.StudentOffice.Queries.GetAtRiskStudents;

public class GetAtRiskStudentsQuery : IRequest<IEnumerable<AtRiskStudentDto>>
{
    public decimal? ScoreAverageThreshold { get; set; } = 60; // Default: below 60% average
    public int? MissedDeadlinesThreshold { get; set; } = 3; // Default: more than 3 missed deadlines
    public decimal? AttendanceThreshold { get; set; } = 70; // Default: below 70% attendance
}

public class AtRiskStudentDto
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public List<string> RiskFactors { get; set; } = new();
    public decimal AverageScore { get; set; }
    public int MissedDeadlinesCount { get; set; }
    public decimal AttendancePercentage { get; set; }
    public int TotalAssignments { get; set; }
    public int CompletedAssignments { get; set; }
    public StudentProgressMetricsDto ProgressMetrics { get; set; } = new();
}

public class StudentProgressMetricsDto
{
    public decimal AverageTimeOnPage { get; set; } // Average time on assignment pages in seconds
    public decimal AverageSubmissionLateness { get; set; } // Average hours late for submissions
    public decimal QuizPassRate { get; set; } // Percentage of quizzes passed
    public int TotalQuizzes { get; set; }
    public int PassedQuizzes { get; set; }
}

