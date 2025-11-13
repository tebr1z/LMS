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

    public override async Task<List<Group>> ListAsync()
    {
        return await _dbSet
            .Include(g => g.GroupUsers)
            .Include(g => g.CourseGroups)
            .ToListAsync();
    }

    public async Task<Group?> GetGroupWithDetailsAsync(int groupId)
    {
        return await _dbSet
            .Include(g => g.CourseGroups)
                .ThenInclude(cg => cg.CoursePrepared)
            .Include(g => g.GroupUsers)
            .FirstOrDefaultAsync(g => g.Id == groupId);
    }

    public async Task<List<Group>> GetGroupsByCourseIdAsync(int coursePreparedId)
    {
        return await _dbSet
            .Where(g => g.CourseGroups.Any(cg => cg.CoursePreparedId == coursePreparedId))
            .Include(g => g.CourseGroups)
                .ThenInclude(cg => cg.CoursePrepared)
            .Include(g => g.GroupUsers)
            .ToListAsync();
    }

    public async Task<bool> IsUserInGroupAsync(int groupId, int userId)
    {
        return await _dbSet
            .AnyAsync(g => g.Id == groupId && g.GroupUsers.Any(gu => gu.UserId == userId));
    }

    public async Task<bool> IsCourseInGroupAsync(int groupId, int coursePreparedId)
    {
        return await _dbSet
            .AnyAsync(g => g.Id == groupId && g.CourseGroups.Any(cg => cg.CoursePreparedId == coursePreparedId));
    }

    public async Task<List<Group>> GetGroupsForUserAsync(int userId, string userRole)
    {
        // This method routes to specific role-based methods
        // For now, return all groups for MasterAdmin/Mentor, or route to specific methods
        if (userRole == "MasterAdmin" || userRole == "Mentor")
        {
            return await ListAsync();
        }
        else if (userRole == "Student")
        {
            return await GetGroupsForStudentAsync(userId);
        }
        else if (userRole == "Teacher")
        {
            return await GetGroupsForTeacherAsync(userId);
        }
        else if (userRole == "Admin")
        {
            return await GetGroupsForAdminAsync(userId);
        }
        return new List<Group>();
    }

    public async Task<List<Group>> GetGroupsForStudentAsync(int studentId)
    {
        // Student sees only groups they belong to
        return await _dbSet
            .Where(g => g.GroupUsers.Any(gu => gu.UserId == studentId))
            .Include(g => g.GroupUsers)
            .Include(g => g.CourseGroups)
            .ToListAsync();
    }

    public async Task<List<Group>> GetGroupsForTeacherAsync(int teacherId)
    {
        // Teacher sees groups they are assigned to (via GroupUsers)
        return await _dbSet
            .Where(g => g.GroupUsers.Any(gu => gu.UserId == teacherId))
            .Include(g => g.GroupUsers)
            .Include(g => g.CourseGroups)
            .ToListAsync();
    }

    public async Task<List<Group>> GetGroupsForAdminAsync(int adminId)
    {
        // Admin sees groups they created
        return await _dbSet
            .Where(g => g.CreatedById == adminId)
            .Include(g => g.GroupUsers)
            .Include(g => g.CourseGroups)
            .ToListAsync();
    }
}

