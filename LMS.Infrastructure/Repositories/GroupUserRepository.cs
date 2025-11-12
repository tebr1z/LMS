using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LMS.Infrastructure.Repositories;

public class GroupUserRepository : EfRepository<GroupUser>, IGroupUserRepository
{
    public GroupUserRepository(LmsDbContext context) : base(context)
    {
    }

    public async Task<List<GroupUser>> GetGroupUsersByGroupIdAsync(int groupId)
    {
        return await _dbSet
            .Where(gu => gu.GroupId == groupId)
            .ToListAsync();
    }

    public async Task<GroupUser?> GetByGroupAndUserAsync(int groupId, int userId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(gu => gu.GroupId == groupId && gu.UserId == userId);
    }

    public async Task<bool> IsUserAssignedToGroupAsync(int groupId, int userId)
    {
        return await _dbSet
            .AnyAsync(gu => gu.GroupId == groupId && gu.UserId == userId);
    }
}

