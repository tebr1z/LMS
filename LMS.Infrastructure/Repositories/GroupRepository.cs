using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LMS.Infrastructure.Repositories;

public class GroupRepository : EfRepository<Group>, IGroupRepository
{
    public GroupRepository(LmsDbContext context) : base(context)
    {
    }

    public async Task<Group?> GetGroupWithDetailsAsync(int groupId)
    {
        return await _dbSet
            .Include(g => g.CourseGroups)
                .ThenInclude(cg => cg.Course)
            .Include(g => g.GroupUsers)
            .FirstOrDefaultAsync(g => g.Id == groupId);
    }

    public async Task<List<Group>> GetGroupsByCourseIdAsync(int courseId)
    {
        return await _dbSet
            .Where(g => g.CourseGroups.Any(cg => cg.CourseId == courseId))
            .Include(g => g.CourseGroups)
            .Include(g => g.GroupUsers)
            .ToListAsync();
    }

    public async Task<bool> IsUserInGroupAsync(int groupId, int userId)
    {
        return await _dbSet
            .AnyAsync(g => g.Id == groupId && g.GroupUsers.Any(gu => gu.UserId == userId));
    }

    public async Task<bool> IsCourseInGroupAsync(int groupId, int courseId)
    {
        return await _dbSet
            .AnyAsync(g => g.Id == groupId && g.CourseGroups.Any(cg => cg.CourseId == courseId));
    }
}

