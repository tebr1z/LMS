using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LMS.Infrastructure.Repositories;

public class AssignmentTelemetryRepository : EfRepository<AssignmentTelemetry>, IAssignmentTelemetryRepository
{
    public AssignmentTelemetryRepository(LmsDbContext context) : base(context)
    {
    }

    public async Task<List<AssignmentTelemetry>> GetTelemetryByStudentAndAssignmentAsync(int studentId, int assignmentId)
    {
        return await _dbSet
            .Where(at => at.StudentId == studentId && at.AssignmentId == assignmentId)
            .OrderBy(at => at.Timestamp)
            .ToListAsync();
    }

    public async Task<List<AssignmentTelemetry>> GetTelemetryBySessionIdAsync(string sessionId)
    {
        return await _dbSet
            .Where(at => at.SessionId == sessionId)
            .OrderBy(at => at.Timestamp)
            .ToListAsync();
    }

    public async Task<int> GetTotalTimeOnPageBySubmissionAsync(int submissionId)
    {
        return await _dbSet
            .Where(at => at.SubmissionId == submissionId)
            .SumAsync(at => at.SecondsActive);
    }

    public async Task<List<AssignmentTelemetry>> GetTelemetryByCourseInstanceAsync(int courseInstanceId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var query = _dbSet
            .Include(at => at.Assignment)
            .Where(at => at.Assignment.CourseInstanceId == courseInstanceId);

        if (fromDate.HasValue)
        {
            query = query.Where(at => at.Timestamp >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(at => at.Timestamp <= toDate.Value);
        }

        return await query.OrderBy(at => at.Timestamp).ToListAsync();
    }
}

