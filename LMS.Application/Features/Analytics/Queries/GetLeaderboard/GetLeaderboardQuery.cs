using MediatR;

namespace LMS.Application.Features.Analytics.Queries.GetLeaderboard;

public class GetLeaderboardQuery : IRequest<IEnumerable<LeaderboardEntryDto>>
{
    public int? CourseInstanceId { get; set; } // Optional: filter by course instance
    public string Period { get; set; } = "overall"; // "week", "month", "overall"
    public int? TopN { get; set; } = 10; // Number of top students to return
}

public class LeaderboardEntryDto
{
    public int Rank { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public decimal AveragePercent { get; set; }
    public int TotalSubmissions { get; set; } // Total assignments + quizzes completed
    public int AssignmentsPassedCount { get; set; }
    public int QuizzesPassedCount { get; set; }
    public DateTime? LastActivity { get; set; }
    public int TotalPoints { get; set; }
    public int TotalPossiblePoints { get; set; }
}

