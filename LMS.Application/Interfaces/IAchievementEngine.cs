namespace LMS.Application.Interfaces;

/// <summary>
/// Service interface for achievement engine
/// </summary>
public interface IAchievementEngine
{
    /// <summary>
    /// Check and award achievements for a user
    /// </summary>
    Task CheckAndAwardAchievementsAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Award points to a user
    /// </summary>
    Task AwardPointsAsync(int userId, int points, string reason, CancellationToken cancellationToken = default);
}

