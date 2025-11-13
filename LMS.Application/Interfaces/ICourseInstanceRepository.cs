using LMS.Domain.Entities;

namespace LMS.Application.Interfaces;

public interface ICourseInstanceRepository : IRepository<CourseInstance>
{
    Task<CourseInstance?> GetByCoursePreparedAndGroupAsync(int coursePreparedId, int groupId);
    Task<List<CourseInstance>> GetByGroupIdAsync(int groupId);
    Task<List<CourseInstance>> GetByCoursePreparedIdAsync(int coursePreparedId);
}

