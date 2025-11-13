using LMS.Domain.Entities;

namespace LMS.Application.Interfaces;

public interface ICourseGroupRepository : IRepository<CourseGroup>
{
    Task<List<CourseGroup>> GetCourseGroupsByGroupIdAsync(int groupId);
    Task<List<CourseGroup>> GetCourseGroupsByCourseIdAsync(int coursePreparedId);
    Task<CourseGroup?> GetByGroupAndCourseAsync(int groupId, int coursePreparedId);
}

