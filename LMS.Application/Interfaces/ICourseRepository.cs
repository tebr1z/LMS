using LMS.Domain.Entities;

namespace LMS.Application.Interfaces;

public interface ICourseRepository : IRepository<Course>
{
    Task<List<Course>> GetCoursesByCreatorAsync(int creatorId);
    Task<Course?> GetCourseWithEnrollmentsAsync(int courseId);
}

