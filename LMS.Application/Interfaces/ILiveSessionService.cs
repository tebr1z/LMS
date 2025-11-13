namespace LMS.Application.Interfaces;

/// <summary>
/// Service interface for live sessions (Jitsi/WebRTC integration)
/// </summary>
public interface ILiveSessionService
{
    /// <summary>
    /// Start a live session for a group
    /// </summary>
    Task<string> StartSessionAsync(int groupId, int teacherId, CancellationToken cancellationToken = default);

    /// <summary>
    /// End a live session
    /// </summary>
    Task EndSessionAsync(int sessionId, int teacherId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current active session for a group with JWT-signed URL
    /// </summary>
    Task<string?> GetCurrentSessionUrlAsync(int groupId, int userId, string userRole, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate JWT token for Jitsi meeting
    /// </summary>
    string GenerateJwtToken(string roomName, string userName, string userRole, bool isModerator);
}

