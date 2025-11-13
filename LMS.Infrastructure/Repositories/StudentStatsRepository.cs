using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LMS.Infrastructure.Repositories;

public class StudentStatsRepository : EfRepository<StudentStats>, IStudentStatsRepository
{
    public StudentStatsRepository(LmsDbContext context) : base(context)
    {
    }

    public async Task<StudentStats?> GetByStudentIdAsync(int studentId, int? courseInstanceId = null)
    {
        return await _dbSet
            .FirstOrDefaultAsync(s => s.StudentId == studentId && s.CourseInstanceId == courseInstanceId);
    }

    public async Task<List<StudentStats>> GetByCourseInstanceIdAsync(int courseInstanceId)
    {
        return await _dbSet
            .Where(s => s.CourseInstanceId == courseInstanceId)
            .OrderByDescending(s => s.AveragePercent)
            .ThenByDescending(s => s.AssignmentsCompletedCount)
            .ToListAsync();
    }

    public async Task<List<StudentStats>> GetTopStudentsByCourseInstanceAsync(int courseInstanceId, int topN = 10)
    {
        return await _dbSet
            .Where(s => s.CourseInstanceId == courseInstanceId)
            .OrderByDescending(s => s.AveragePercent)
            .ThenByDescending(s => s.AssignmentsCompletedCount)
            .ThenByDescending(s => s.LastActivity)
            .Take(topN)
            .ToListAsync();
    }
}

