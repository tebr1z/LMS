using LMS.Domain.Entities;

namespace LMS.Application.Interfaces;

/// <summary>
/// Repository interface for LearningLevel operations
/// </summary>
public interface ILearningLevelRepository : IRepository<LearningLevel>
{
    /// <summary>
    /// Get learning level for a student
    /// </summary>
    Task<LearningLevel?> GetByStudentIdAsync(int studentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get or create learning level for a student (defaults to Medium)
    /// </summary>
    Task<LearningLevel> GetOrCreateAsync(int studentId, CancellationToken cancellationToken = default);
}

