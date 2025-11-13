using LMS.Domain.Entities;

namespace LMS.Application.Interfaces;

public interface ILiveSessionRepository : IRepository<LiveSession>
{
    Task<LiveSession?> GetActiveSessionByGroupIdAsync(int groupId, CancellationToken cancellationToken = default);
    Task<List<LiveSession>> GetSessionsByGroupIdAsync(int groupId, CancellationToken cancellationToken = default);
    Task<List<LiveSession>> GetSessionsByTeacherIdAsync(int teacherId, CancellationToken cancellationToken = default);
    Task<LiveSession?> GetSessionWithDetailsAsync(int sessionId, CancellationToken cancellationToken = default);
}

