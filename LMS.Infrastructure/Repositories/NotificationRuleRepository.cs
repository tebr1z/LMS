using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Domain.Enums;
using LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LMS.Infrastructure.Repositories;

public class NotificationRuleRepository : EfRepository<NotificationRule>, INotificationRuleRepository
{
    public NotificationRuleRepository(LmsDbContext context) : base(context)
    {
    }

    public async Task<List<NotificationRule>> GetActiveRulesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.IsActive)
            .OrderBy(r => r.Priority ?? 5) // Order by priority (default 5 if not set)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<NotificationRule>> GetRulesByRoleAsync(UserRole role, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.IsActive && r.TargetRole == role)
            .OrderBy(r => r.Priority ?? 5)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<NotificationRule>> GetRulesByTypeAsync(string type, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(r => r.IsActive && r.Type == type)
            .OrderBy(r => r.Priority ?? 5)
            .ToListAsync(cancellationToken);
    }
}


