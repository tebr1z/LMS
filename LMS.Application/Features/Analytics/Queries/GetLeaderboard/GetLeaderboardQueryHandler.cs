using LMS.Application.Interfaces;
using MediatR;

namespace LMS.Application.Features.Analytics.Queries.GetLeaderboard;

public class GetLeaderboardQueryHandler : IRequestHandler<GetLeaderboardQuery, IEnumerable<LeaderboardEntryDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;

    public GetLeaderboardQueryHandler(IUnitOfWork unitOfWork, IUserRepository userRepository)
    {
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
    }

    public async Task<IEnumerable<LeaderboardEntryDto>> Handle(GetLeaderboardQuery request, CancellationToken cancellationToken)
    {
        // Determine date range based on period
        DateTime? fromDate = null;
        var now = DateTime.UtcNow;

        switch (request.Period?.ToLower())
        {
            case "week":
                fromDate = now.AddDays(-7);
                break;
            case "month":
                fromDate = now.AddDays(-30);
                break;
            case "overall":
            default:
                fromDate = null; // No date filter
                break;
        }

        // Get student stats for the course instance
        List<Domain.Entities.StudentStats> statsList;
        
        if (request.CourseInstanceId.HasValue)
        {
            statsList = await _unitOfWork.StudentStats.GetByCourseInstanceIdAsync(request.CourseInstanceId.Value);
        }
        else
        {
            // Get all stats (overall leaderboard)
            var allStats = await _unitOfWork.StudentStats.ListAsync();
            statsList = allStats
                .Where(s => s.CourseInstanceId == null) // Overall stats only
                .ToList();
        }

        // Filter by date if period is specified
        if (fromDate.HasValue)
        {
            statsList = statsList
                .Where(s => s.LastActivity.HasValue && s.LastActivity.Value >= fromDate.Value)
                .ToList();
        }

        // Get user names
        var allUsers = await _userRepository.ListAsync();
        var userLookup = new Dictionary<int, string>();
        foreach (var user in allUsers)
        {
            var fullName = await _userRepository.GetUserFullNameAsync(user.Id);
            userLookup[user.Id] = fullName ?? "Unknown";
        }

        // Map to DTOs and calculate rankings
        var leaderboardEntries = new List<LeaderboardEntryDto>();
        var rank = 1;

        // Get additional submission counts if needed
        var allSubmissions = await _unitOfWork.AssignmentSubmissions.ListAsync();
        var allQuizSessions = await _unitOfWork.QuizSessions.ListAsync();

        foreach (var stats in statsList.OrderByDescending(s => s.AveragePercent)
                                       .ThenByDescending(s => s.AssignmentsCompletedCount)
                                       .ThenByDescending(s => s.LastActivity))
        {
            // Filter submissions by date if needed
            var studentSubmissions = allSubmissions
                .Where(s => s.StudentId == stats.StudentId)
                .ToList();

            var studentQuizzes = allQuizSessions
                .Where(qs => qs.StudentId == stats.StudentId)
                .ToList();

            if (fromDate.HasValue)
            {
                studentSubmissions = studentSubmissions
                    .Where(s => (s.UpdatedAt ?? s.CreatedAt) >= fromDate.Value)
                    .ToList();

                studentQuizzes = studentQuizzes
                    .Where(qs => (qs.UpdatedAt ?? qs.CreatedAt) >= fromDate.Value)
                    .ToList();
            }

            var totalSubmissions = studentSubmissions.Count(s => s.Score.HasValue || !string.IsNullOrEmpty(s.AnswerText) || !string.IsNullOrEmpty(s.FileUrl)) +
                                  studentQuizzes.Count(qs => qs.IsCompleted);

            leaderboardEntries.Add(new LeaderboardEntryDto
            {
                Rank = rank++,
                StudentId = stats.StudentId,
                StudentName = userLookup.ContainsKey(stats.StudentId) ? userLookup[stats.StudentId] : "Unknown",
                AveragePercent = stats.AveragePercent,
                TotalSubmissions = totalSubmissions,
                AssignmentsPassedCount = stats.AssignmentsPassedCount,
                QuizzesPassedCount = stats.QuizzesPassedCount,
                LastActivity = stats.LastActivity,
                TotalPoints = stats.TotalPoints,
                TotalPossiblePoints = stats.TotalPossiblePoints
            });

            // Stop if we've reached TopN
            if (request.TopN.HasValue && leaderboardEntries.Count >= request.TopN.Value)
            {
                break;
            }
        }

        return leaderboardEntries;
    }
}

