using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LMS.Infrastructure.Repositories;

public class AssignmentFeedbackAIRepository : EfRepository<AssignmentFeedbackAI>, IAssignmentFeedbackAIRepository
{
    public AssignmentFeedbackAIRepository(LmsDbContext context) : base(context)
    {
    }

    public async Task<AssignmentFeedbackAI?> GetLatestBySubmissionIdAsync(int submissionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(af => af.Submission)
            .Where(af => af.SubmissionId == submissionId)
            .OrderByDescending(af => af.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<AssignmentFeedbackAI>> GetBySubmissionIdAsync(int submissionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(af => af.Submission)
            .Where(af => af.SubmissionId == submissionId)
            .OrderByDescending(af => af.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsForSubmissionAsync(int submissionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(af => af.SubmissionId == submissionId, cancellationToken);
    }
}

