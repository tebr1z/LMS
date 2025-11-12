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
            .Include(cg => cg.Course)
            .ToListAsync();
    }

    public async Task<List<CourseGroup>> GetCourseGroupsByCourseIdAsync(int courseId)
    {
        return await _dbSet
            .Where(cg => cg.CourseId == courseId)
            .Include(cg => cg.Group)
            .ToListAsync();
    }

    public async Task<CourseGroup?> GetByGroupAndCourseAsync(int groupId, int courseId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(cg => cg.GroupId == groupId && cg.CourseId == courseId);
    }
}

