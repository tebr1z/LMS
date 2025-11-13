using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LMS.Infrastructure.Repositories;

public class AssignmentRepository : EfRepository<Assignment>, IAssignmentRepository
{
    public AssignmentRepository(LmsDbContext context) : base(context)
    {
    }

    public async Task<List<Assignment>> GetAssignmentsByCourseIdAsync(int courseId)
    {
        return await _dbSet
            .Where(a => a.CourseId == courseId)
            .Include(a => a.Submissions)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Assignment>> GetAssignmentsByCoursePreparedIdAsync(int coursePreparedId)
    {
        return await _dbSet
            .Where(a => a.CoursePreparedId == coursePreparedId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<Assignment?> GetAssignmentWithSubmissionsAsync(int assignmentId)
    {
        return await _dbSet
            .Include(a => a.Submissions)
            .Include(a => a.Course)
            .FirstOrDefaultAsync(a => a.Id == assignmentId);
    }

    public async Task<bool> IsDeadlinePassedAsync(int assignmentId)
    {
        var assignment = await _dbSet.FindAsync(assignmentId);
        if (assignment == null)
        {
            return true; // If assignment doesn't exist, consider deadline passed
        }
        return assignment.Deadline < DateTime.UtcNow;
    }
}

