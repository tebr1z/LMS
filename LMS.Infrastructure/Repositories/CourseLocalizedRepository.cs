using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LMS.Infrastructure.Repositories;

public class CourseLocalizedRepository : EfRepository<CourseLocalized>, ICourseLocalizedRepository
{
    public CourseLocalizedRepository(LmsDbContext context) : base(context)
    {
    }

    public async Task<CourseLocalized?> GetByCourseIdAndLangAsync(int courseId, string langCode, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(cl => cl.Course)
            .FirstOrDefaultAsync(cl => cl.CourseId == courseId && cl.LangCode == langCode, cancellationToken);
    }

    public async Task<List<CourseLocalized>> GetByCourseIdAsync(int courseId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(cl => cl.Course)
            .Where(cl => cl.CourseId == courseId)
            .OrderBy(cl => cl.LangCode)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(int courseId, string langCode, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(cl => cl.CourseId == courseId && cl.LangCode == langCode, cancellationToken);
    }

    public async Task<List<string>> GetSupportedLanguagesForCourseAsync(int courseId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(cl => cl.CourseId == courseId)
            .Select(cl => cl.LangCode)
            .OrderBy(lang => lang)
            .ToListAsync(cancellationToken);
    }
}

