using LMS.Domain.Entities;

namespace LMS.Application.Interfaces;

/// <summary>
/// Repository interface for AuditLog operations
/// </summary>
public interface IAuditLogRepository : IRepository<AuditLog>
{
    /// <summary>
    /// Get audit logs with optional filtering
    /// </summary>
    Task<List<AuditLog>> GetAuditLogsAsync(
        string? entity = null,
        int? entityId = null,
        string? action = null,
        int? userId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int? pageNumber = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audit logs for a specific entity
    /// </summary>
    Task<List<AuditLog>> GetAuditLogsByEntityAsync(string entity, int entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audit logs by user
    /// </summary>
    Task<List<AuditLog>> GetAuditLogsByUserAsync(int userId, CancellationToken cancellationToken = default);
}

