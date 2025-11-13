using LMS.Application.Interfaces;
using LMS.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace LMS.Infrastructure.Services;

/// <summary>
/// Service implementation for adaptive learning
/// </summary>
public class AdaptiveLearningService : IAdaptiveLearningService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AdaptiveLearningService> _logger;

    public AdaptiveLearningService(
        IUnitOfWork unitOfWork,
        ILogger<AdaptiveLearningService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task AnalyzeAndUpdateLearningLevelAsync(int studentId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get last 5 submissions for the student (excluding ReadingMaterial)
            var allSubmissions = await _unitOfWork.AssignmentSubmissions.ListAsync();
            var allAssignments = await _unitOfWork.Assignments.ListAsync();

            var studentSubmissions = allSubmissions
                .Where(s => s.StudentId == studentId && s.Score.HasValue)
                .OrderByDescending(s => s.SubmittedAt)
                .Take(5)
                .ToList();

            if (!studentSubmissions.Any())
            {
                // No submissions yet, keep default (Medium)
                _logger.LogInformation("Student {StudentId} has no scored submissions yet. Keeping default difficulty.", studentId);
                return;
            }

            // Create assignment type lookup
            var assignmentTypeLookup = allAssignments.ToDictionary(a => a.Id, a => a.AssignmentType);

            // Filter out ReadingMaterial submissions and calculate average
            var scoredSubmissions = studentSubmissions
                .Where(s => assignmentTypeLookup.ContainsKey(s.AssignmentId) &&
                           assignmentTypeLookup[s.AssignmentId] != AssignmentType.ReadingMaterial)
                .ToList();

            if (!scoredSubmissions.Any())
            {
                _logger.LogInformation("Student {StudentId} has no scored submissions (excluding ReadingMaterial). Keeping default difficulty.", studentId);
                return;
            }

            // Calculate average percentage score
            var averageScore = scoredSubmissions.Average(s => s.PercentageScore);

            // Determine difficulty level based on average score
            int difficultyLevel;
            if (averageScore < 60)
            {
                difficultyLevel = 1; // Easy
            }
            else if (averageScore >= 60 && averageScore <= 85)
            {
                difficultyLevel = 2; // Medium
            }
            else
            {
                difficultyLevel = 3; // Hard
            }

            // Get or create learning level for student
            var learningLevel = await _unitOfWork.LearningLevels.GetOrCreateAsync(studentId, cancellationToken);

            // Update difficulty level if changed
            if (learningLevel.DifficultyLevel != difficultyLevel)
            {
                var oldLevel = learningLevel.DifficultyLevel;
                learningLevel.DifficultyLevel = difficultyLevel;
                learningLevel.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.LearningLevels.UpdateAsync(learningLevel);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation(
                    "Updated learning level for student {StudentId}: {OldLevel} -> {NewLevel} (average score: {AverageScore:F2}%)",
                    studentId, oldLevel, difficultyLevel, averageScore);
            }
            else
            {
                _logger.LogInformation(
                    "Learning level for student {StudentId} unchanged: {Level} (average score: {AverageScore:F2}%)",
                    studentId, difficultyLevel, averageScore);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error analyzing and updating learning level for student {StudentId}", studentId);
            // Don't throw - this should not break the main operation
        }
    }

    public async Task<int> GetRecommendedDifficultyAsync(int studentId, CancellationToken cancellationToken = default)
    {
        var learningLevel = await _unitOfWork.LearningLevels.GetOrCreateAsync(studentId, cancellationToken);
        return learningLevel.DifficultyLevel;
    }

    public async Task<Domain.Entities.LearningLevel?> GetLearningLevelAsync(int studentId, CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.LearningLevels.GetByStudentIdAsync(studentId, cancellationToken);
    }
}

