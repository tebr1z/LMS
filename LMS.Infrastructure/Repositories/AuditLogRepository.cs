using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LMS.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for AuditLog
/// </summary>
public class AuditLogRepository : EfRepository<AuditLog>, IAuditLogRepository
{
    public AuditLogRepository(LmsDbContext context) : base(context)
    {
    }

    public async Task<List<AuditLog>> GetAuditLogsAsync(
        string? entity = null,
        int? entityId = null,
        string? action = null,
        int? userId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int? pageNumber = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if (!string.IsNullOrEmpty(entity))
        {
            query = query.Where(a => a.Entity == entity);
        }

        if (entityId.HasValue)
        {
            query = query.Where(a => a.EntityId == entityId.Value);
        }

        if (!string.IsNullOrEmpty(action))
        {
            query = query.Where(a => a.Action == action);
        }

        if (userId.HasValue)
        {
            query = query.Where(a => a.UserId == userId.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(a => a.Timestamp >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(a => a.Timestamp <= endDate.Value);
        }

        // Order by timestamp descending (newest first)
        query = query.OrderByDescending(a => a.Timestamp);

        // Apply pagination if provided
        if (pageNumber.HasValue && pageSize.HasValue)
        {
            query = query.Skip((pageNumber.Value - 1) * pageSize.Value).Take(pageSize.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<List<AuditLog>> GetAuditLogsByEntityAsync(string entity, int entityId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(a => a.Entity == entity && a.EntityId == entityId)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<AuditLog>> GetAuditLogsByUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync(cancellationToken);
    }
}

