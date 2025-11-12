using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LMS.Infrastructure.Repositories;

public class AssignmentSubmissionRepository : EfRepository<AssignmentSubmission>, IAssignmentSubmissionRepository
{
    public AssignmentSubmissionRepository(LmsDbContext context) : base(context)
    {
    }

    public async Task<AssignmentSubmission?> GetSubmissionByAssignmentAndStudentAsync(int assignmentId, int studentId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(s => s.AssignmentId == assignmentId && s.StudentId == studentId);
    }

    public async Task<List<AssignmentSubmission>> GetSubmissionsByAssignmentIdAsync(int assignmentId)
    {
        return await _dbSet
            .Where(s => s.AssignmentId == assignmentId)
            .OrderByDescending(s => s.SubmittedAt)
            .ToListAsync();
    }

    public async Task<List<AssignmentSubmission>> GetSubmissionsByStudentIdAsync(int studentId)
    {
        return await _dbSet
            .Where(s => s.StudentId == studentId)
            .Include(s => s.Assignment)
            .OrderByDescending(s => s.SubmittedAt)
            .ToListAsync();
    }
}

