using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LMS.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for LearningLevel
/// </summary>
public class LearningLevelRepository : EfRepository<LearningLevel>, ILearningLevelRepository
{
    public LearningLevelRepository(LmsDbContext context) : base(context)
    {
    }

    public async Task<LearningLevel?> GetByStudentIdAsync(int studentId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(l => l.StudentId == studentId, cancellationToken);
    }

    public async Task<LearningLevel> GetOrCreateAsync(int studentId, CancellationToken cancellationToken = default)
    {
        var learningLevel = await GetByStudentIdAsync(studentId, cancellationToken);
        
        if (learningLevel == null)
        {
            learningLevel = new LearningLevel
            {
                StudentId = studentId,
                DifficultyLevel = 2, // Default: Medium
                CreatedAt = DateTime.UtcNow
                // UpdatedAt is inherited from BaseEntity and will be set when updated
            };
            await AddAsync(learningLevel);
        }

        return learningLevel;
    }
}

