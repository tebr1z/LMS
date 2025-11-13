using MediatR;

namespace LMS.Application.Features.Analytics.Queries.GetTopEngagedStudents;

public class GetTopEngagedStudentsQuery : IRequest<IEnumerable<EngagedStudentDto>>
{
    public int? CourseId { get; set; } // Optional: filter by course
    public DateTime? FromDate { get; set; } // Optional: filter from date
    public DateTime? ToDate { get; set; } // Optional: filter to date
    public int? TopN { get; set; } = 10; // Number of top students to return
}

public class EngagedStudentDto
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public decimal TotalTimeOnPageInSeconds { get; set; }
    public decimal TotalTimeOnPageInHours { get; set; } // Calculated from seconds
    public int TotalAssignmentsCompleted { get; set; }
    public int TotalQuizzesAttempted { get; set; }
    public int TotalQuizzesPassed { get; set; }
    public decimal QuizPassRate { get; set; } // Percentage
    public decimal AverageScore { get; set; }
    public EngagementScoreDto EngagementScore { get; set; } = new();
}

public class EngagementScoreDto
{
    public decimal TimeScore { get; set; } // Normalized time score (0-100)
    public decimal ActivityScore { get; set; } // Normalized activity score (0-100)
    public decimal PerformanceScore { get; set; } // Normalized performance score (0-100)
    public decimal OverallScore { get; set; } // Combined engagement score (0-100)
}

