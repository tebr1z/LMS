using LMS.Domain.Entities;

namespace LMS.Application.Interfaces;

public interface IAssignmentFeedbackAIRepository : IRepository<AssignmentFeedbackAI>
{
    Task<AssignmentFeedbackAI?> GetLatestBySubmissionIdAsync(int submissionId, CancellationToken cancellationToken = default);
    Task<List<AssignmentFeedbackAI>> GetBySubmissionIdAsync(int submissionId, CancellationToken cancellationToken = default);
    Task<bool> ExistsForSubmissionAsync(int submissionId, CancellationToken cancellationToken = default);
}

