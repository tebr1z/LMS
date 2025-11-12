using LMS.Domain.Entities;

namespace LMS.Application.Interfaces;

public interface IEnrollmentRepository : IRepository<Enrollment>
{
    Task<bool> IsUserEnrolledAsync(int userId, int courseId);
    Task<List<Enrollment>> GetEnrollmentsByUserAsync(int userId);
    Task<List<Enrollment>> GetEnrollmentsByCourseAsync(int courseId);
}

