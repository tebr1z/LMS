using LMS.Domain.Entities;

namespace LMS.Application.Interfaces;

public interface IAssignmentSubmissionRepository : IRepository<AssignmentSubmission>
{
    Task<AssignmentSubmission?> GetSubmissionByAssignmentAndStudentAsync(int assignmentId, int studentId);
    Task<List<AssignmentSubmission>> GetSubmissionsByAssignmentIdAsync(int assignmentId);
    Task<List<AssignmentSubmission>> GetSubmissionsByStudentIdAsync(int studentId);
}

