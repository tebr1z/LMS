using LMS.Domain.Entities;

namespace LMS.Application.Interfaces;

public interface IAssignmentRepository : IRepository<Assignment>
{
    Task<List<Assignment>> GetAssignmentsByCourseIdAsync(int courseId);
    Task<Assignment?> GetAssignmentWithSubmissionsAsync(int assignmentId);
    Task<bool> IsDeadlinePassedAsync(int assignmentId);
}

