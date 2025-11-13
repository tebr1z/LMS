using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LMS.Infrastructure.Repositories;

public class CourseInstanceRepository : EfRepository<CourseInstance>, ICourseInstanceRepository
{
    public CourseInstanceRepository(LmsDbContext context) : base(context)
    {
    }

    public async Task<CourseInstance?> GetByCoursePreparedAndGroupAsync(int coursePreparedId, int groupId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(ci => ci.CoursePreparedId == coursePreparedId && ci.GroupId == groupId);
    }

    public async Task<List<CourseInstance>> GetByGroupIdAsync(int groupId)
    {
        return await _dbSet
            .Where(ci => ci.GroupId == groupId)
            .Include(ci => ci.CoursePrepared)
            .ToListAsync();
    }

    public async Task<List<CourseInstance>> GetByCoursePreparedIdAsync(int coursePreparedId)
    {
        return await _dbSet
            .Where(ci => ci.CoursePreparedId == coursePreparedId)
            .Include(ci => ci.Group)
            .ToListAsync();
    }
}

