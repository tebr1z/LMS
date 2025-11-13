namespace LMS.Application.Interfaces;

/// <summary>
/// Service interface for adaptive learning
/// </summary>
public interface IAdaptiveLearningService
{
    /// <summary>
    /// Analyze student performance and update learning level
    /// </summary>
    Task AnalyzeAndUpdateLearningLevelAsync(int studentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get recommended difficulty level for a student
    /// </summary>
    Task<int> GetRecommendedDifficultyAsync(int studentId, CancellationToken cancellationToken = default);
}

