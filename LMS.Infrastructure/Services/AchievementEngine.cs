using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LMS.Infrastructure.Services;

/// <summary>
/// Service implementation for achievement engine
/// </summary>
public class AchievementEngine : IAchievementEngine
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AchievementEngine> _logger;

    public AchievementEngine(
        IUnitOfWork unitOfWork,
        ILogger<AchievementEngine> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task CheckAndAwardAchievementsAsync(int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get all active achievements
            var achievements = await _unitOfWork.Achievements.GetActiveAchievementsAsync(cancellationToken);

            foreach (var achievement in achievements)
            {
                // Skip if user already has this achievement
                if (await _unitOfWork.UserAchievements.HasAchievementAsync(userId, achievement.Id, cancellationToken))
                {
                    continue;
                }

                // Check if user meets the achievement criteria
                if (await MeetsAchievementCriteriaAsync(userId, achievement, cancellationToken))
                {
                    // Award the achievement
                    var userAchievement = new UserAchievement
                    {
                        UserId = userId,
                        AchievementId = achievement.Id,
                        EarnedAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _unitOfWork.UserAchievements.AddAsync(userAchievement);
                    await _unitOfWork.SaveChangesAsync();

                    // Award points if configured
                    if (achievement.PointsReward > 0)
                    {
                        await AwardPointsAsync(userId, achievement.PointsReward, $"Achievement earned: {achievement.Name}", cancellationToken);
                    }

                    _logger.LogInformation(
                        "Awarded achievement '{AchievementName}' (ID: {AchievementId}) to user {UserId}",
                        achievement.Name, achievement.Id, userId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking and awarding achievements for user {UserId}", userId);
            // Don't throw - this should not break the main operation
        }
    }

    private async Task<bool> MeetsAchievementCriteriaAsync(int userId, Achievement achievement, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(achievement.CriteriaJson))
        {
            return false;
        }

        try
        {
            var criteria = JsonSerializer.Deserialize<Dictionary<string, object>>(achievement.CriteriaJson);
            if (criteria == null)
            {
                return false;
            }

            // Check criteria type
            if (criteria.TryGetValue("type", out var typeObj))
            {
                var type = typeObj?.ToString()?.ToLowerInvariant();

                switch (type)
                {
                    case "submissions_ontime":
                        return await CheckSubmissionsOnTimeAsync(userId, criteria, cancellationToken);

                    case "average_score":
                        return await CheckAverageScoreAsync(userId, criteria, cancellationToken);

                    case "submissions_count":
                        return await CheckSubmissionsCountAsync(userId, criteria, cancellationToken);

                    case "quiz_perfect":
                        return await CheckQuizPerfectAsync(userId, criteria, cancellationToken);

                    case "attendance_percentage":
                        return await CheckAttendancePercentageAsync(userId, criteria, cancellationToken);

                    default:
                        _logger.LogWarning("Unknown achievement criteria type: {Type}", type);
                        return false;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing achievement criteria for achievement {AchievementId}", achievement.Id);
        }

        return false;
    }

    private async Task<bool> CheckSubmissionsOnTimeAsync(int userId, Dictionary<string, object> criteria, CancellationToken cancellationToken)
    {
        if (!criteria.TryGetValue("count", out var countObj) ||
            !int.TryParse(countObj?.ToString(), out var requiredCount))
        {
            return false;
        }

        var allSubmissions = await _unitOfWork.AssignmentSubmissions.ListAsync();
        var allAssignments = await _unitOfWork.Assignments.ListAsync();

        var assignmentLookup = allAssignments.ToDictionary(a => a.Id);

        var onTimeSubmissions = allSubmissions
            .Where(s => s.StudentId == userId && 
                       s.Score.HasValue &&
                       assignmentLookup.ContainsKey(s.AssignmentId))
            .Where(s =>
            {
                var assignment = assignmentLookup[s.AssignmentId];
                return assignment.Deadline.HasValue && 
                       s.SubmittedAt <= assignment.Deadline.Value;
            })
            .Take(requiredCount)
            .Count();

        return onTimeSubmissions >= requiredCount;
    }

    private async Task<bool> CheckAverageScoreAsync(int userId, Dictionary<string, object> criteria, CancellationToken cancellationToken)
    {
        if (!criteria.TryGetValue("minAverage", out var minAverageObj) ||
            !decimal.TryParse(minAverageObj?.ToString(), out var minAverage))
        {
            return false;
        }

        var allSubmissions = await _unitOfWork.AssignmentSubmissions.ListAsync();
        var allAssignments = await _unitOfWork.Assignments.ListAsync();

        var assignmentTypeLookup = allAssignments.ToDictionary(a => a.Id, a => a.AssignmentType);

        var scoredSubmissions = allSubmissions
            .Where(s => s.StudentId == userId && 
                       s.Score.HasValue &&
                       assignmentTypeLookup.ContainsKey(s.AssignmentId) &&
                       assignmentTypeLookup[s.AssignmentId] != AssignmentType.ReadingMaterial)
            .ToList();

        if (!scoredSubmissions.Any())
        {
            return false;
        }

        var averageScore = scoredSubmissions.Average(s => s.PercentageScore);
        return averageScore >= minAverage;
    }

    private async Task<bool> CheckSubmissionsCountAsync(int userId, Dictionary<string, object> criteria, CancellationToken cancellationToken)
    {
        if (!criteria.TryGetValue("count", out var countObj) ||
            !int.TryParse(countObj?.ToString(), out var requiredCount))
        {
            return false;
        }

        var allSubmissions = await _unitOfWork.AssignmentSubmissions.ListAsync();
        var submissionsCount = allSubmissions
            .Count(s => s.StudentId == userId && s.Score.HasValue);

        return submissionsCount >= requiredCount;
    }

    private async Task<bool> CheckQuizPerfectAsync(int userId, Dictionary<string, object> criteria, CancellationToken cancellationToken)
    {
        var allQuizSessions = await _unitOfWork.QuizSessions.ListAsync();

        var perfectSessions = allQuizSessions
            .Any(qs => qs.StudentId == userId && 
                      qs.IsCompleted && 
                      qs.Passed == true &&
                      qs.PercentageScore == 100);

        return perfectSessions;
    }

    private async Task<bool> CheckAttendancePercentageAsync(int userId, Dictionary<string, object> criteria, CancellationToken cancellationToken)
    {
        if (!criteria.TryGetValue("minPercentage", out var minPercentageObj) ||
            !decimal.TryParse(minPercentageObj?.ToString(), out var minPercentage))
        {
            return false;
        }

        var allAttendances = await _unitOfWork.Attendances.ListAsync();
        var studentAttendances = allAttendances
            .Where(a => a.StudentId == userId)
            .ToList();

        if (!studentAttendances.Any())
        {
            return false;
        }

        var attendedCount = studentAttendances.Count(a => a.Present);
        var totalCount = studentAttendances.Count;
        var attendancePercentage = (decimal)attendedCount / totalCount * 100;

        return attendancePercentage >= minPercentage;
    }

    public async Task AwardPointsAsync(int userId, int points, string reason, CancellationToken cancellationToken = default)
    {
        try
        {
            var rewardPoint = new RewardPoint
            {
                UserId = userId,
                Points = points,
                Reason = reason,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.RewardPoints.AddAsync(rewardPoint);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Awarded {Points} points to user {UserId}. Reason: {Reason}",
                points, userId, reason);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error awarding points to user {UserId}", userId);
            // Don't throw - this should not break the main operation
        }
    }
}

