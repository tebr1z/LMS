using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LMS.Infrastructure.Repositories;

public class CourseGroupRepository : EfRepository<CourseGroup>, ICourseGroupRepository
{
    public CourseGroupRepository(LmsDbContext context) : base(context)
    {
    }

    public async Task<List<CourseGroup>> GetCourseGroupsByGroupIdAsync(int groupId)
    {
        return await _dbSet
            .Where(cg => cg.GroupId == groupId)
            .Include(cg => cg.CoursePrepared)
            .ToListAsync();
    }

    public async Task<List<CourseGroup>> GetCourseGroupsByCourseIdAsync(int coursePreparedId)
    {
        return await _dbSet
            .Where(cg => cg.CoursePreparedId == coursePreparedId)
            .Include(cg => cg.Group)
            .Include(cg => cg.CoursePrepared)
            .ToListAsync();
    }

    public async Task<CourseGroup?> GetByGroupAndCourseAsync(int groupId, int coursePreparedId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(cg => cg.GroupId == groupId && cg.CoursePreparedId == coursePreparedId);
    }
}

