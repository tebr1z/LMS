using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LMS.Infrastructure.Repositories;

public class LiveSessionRepository : EfRepository<LiveSession>, ILiveSessionRepository
{
    public LiveSessionRepository(LmsDbContext context) : base(context)
    {
    }

    public async Task<LiveSession?> GetActiveSessionByGroupIdAsync(int groupId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(ls => ls.Group)
            .Where(ls => ls.GroupId == groupId && ls.IsActive && ls.EndTime == null)
            .OrderByDescending(ls => ls.StartTime)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<LiveSession>> GetSessionsByGroupIdAsync(int groupId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(ls => ls.Group)
            .Where(ls => ls.GroupId == groupId)
            .OrderByDescending(ls => ls.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<LiveSession>> GetSessionsByTeacherIdAsync(int teacherId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(ls => ls.Group)
            .Where(ls => ls.TeacherId == teacherId)
            .OrderByDescending(ls => ls.StartTime)
            .ToListAsync(cancellationToken);
    }

    public async Task<LiveSession?> GetSessionWithDetailsAsync(int sessionId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(ls => ls.Group)
            .FirstOrDefaultAsync(ls => ls.Id == sessionId, cancellationToken);
    }
}

