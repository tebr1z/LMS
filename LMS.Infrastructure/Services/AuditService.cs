using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LMS.Infrastructure.Services;

/// <summary>
/// Service implementation for audit logging
/// </summary>
public class AuditService : IAuditService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AuditService> _logger;

    public AuditService(IUnitOfWork unitOfWork, ILogger<AuditService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task LogAuditAsync(
        string entity,
        int entityId,
        string action,
        int userId,
        object? oldValue = null,
        object? newValue = null,
        string? description = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var auditLog = new AuditLog
            {
                Entity = entity,
                EntityId = entityId,
                Action = action,
                UserId = userId,
                OldValue = oldValue != null ? JsonSerializer.Serialize(oldValue) : null,
                NewValue = newValue != null ? JsonSerializer.Serialize(newValue) : null,
                Description = description,
                Timestamp = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.AuditLogs.AddAsync(auditLog);
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging audit entry for {Entity} {EntityId} action {Action}", entity, entityId, action);
            // Don't throw - audit logging should not break the main operation
        }
    }
}

