using LMS.Domain.Entities;

namespace LMS.Application.Interfaces;

public interface IStudentStatsRepository : IRepository<StudentStats>
{
    Task<StudentStats?> GetByStudentIdAsync(int studentId, int? courseInstanceId = null);
    Task<List<StudentStats>> GetByCourseInstanceIdAsync(int courseInstanceId);
    Task<List<StudentStats>> GetTopStudentsByCourseInstanceAsync(int courseInstanceId, int topN = 10);
}

