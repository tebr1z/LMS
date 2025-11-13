using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LMS.Infrastructure.Repositories;

public class AttendanceRepository : EfRepository<Attendance>, IAttendanceRepository
{
    public AttendanceRepository(LmsDbContext context) : base(context)
    {
    }

    public async Task<List<Attendance>> GetAttendancesByGroupIdAsync(int groupId)
    {
        return await _dbSet
            .Where(a => a.GroupId == groupId)
            .OrderByDescending(a => a.Date)
            .ThenBy(a => a.StudentId)
            .ToListAsync();
    }

    public async Task<List<Attendance>> GetAttendancesByGroupAndDateAsync(int groupId, DateTime date)
    {
        return await _dbSet
            .Where(a => a.GroupId == groupId && a.Date.Date == date.Date)
            .OrderBy(a => a.StudentId)
            .ToListAsync();
    }

    public async Task<List<Attendance>> GetAttendancesByStudentIdAsync(int studentId)
    {
        return await _dbSet
            .Where(a => a.StudentId == studentId)
            .OrderByDescending(a => a.Date)
            .ToListAsync();
    }

    public async Task<Attendance?> GetAttendanceByGroupStudentAndDateAsync(int groupId, int studentId, DateTime date)
    {
        return await _dbSet
            .FirstOrDefaultAsync(a => a.GroupId == groupId && a.StudentId == studentId && a.Date.Date == date.Date);
    }
}

