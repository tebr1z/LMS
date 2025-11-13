namespace LMS.Application.Interfaces;

/// <summary>
/// Service interface for audit logging
/// </summary>
public interface IAuditService
{
    /// <summary>
    /// Log an audit entry
    /// </summary>
    Task LogAuditAsync(
        string entity,
        int entityId,
        string action,
        int userId,
        object? oldValue = null,
        object? newValue = null,
        string? description = null,
        CancellationToken cancellationToken = default);
}

