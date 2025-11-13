using LMS.Domain.Entities;

namespace LMS.Application.Interfaces;

public interface IAssignmentTelemetryRepository : IRepository<AssignmentTelemetry>
{
    Task<List<AssignmentTelemetry>> GetTelemetryByStudentAndAssignmentAsync(int studentId, int assignmentId);
    Task<List<AssignmentTelemetry>> GetTelemetryBySessionIdAsync(string sessionId);
    Task<int> GetTotalTimeOnPageBySubmissionAsync(int submissionId);
    Task<List<AssignmentTelemetry>> GetTelemetryByCourseInstanceAsync(int courseInstanceId, DateTime? fromDate = null, DateTime? toDate = null);
}

