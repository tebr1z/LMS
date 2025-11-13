using LMS.Domain.Entities;

namespace LMS.Application.Interfaces;

public interface ICourseLocalizedRepository : IRepository<CourseLocalized>
{
    Task<CourseLocalized?> GetByCourseIdAndLangAsync(int courseId, string langCode, CancellationToken cancellationToken = default);
    Task<List<CourseLocalized>> GetByCourseIdAsync(int courseId, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int courseId, string langCode, CancellationToken cancellationToken = default);
    Task<List<string>> GetSupportedLanguagesForCourseAsync(int courseId, CancellationToken cancellationToken = default);
}

