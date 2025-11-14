using LMS.Domain.Entities;
using LMS.Domain.Enums;

namespace LMS.Application.Interfaces;

public interface INotificationRuleRepository : IRepository<NotificationRule>
{
    Task<List<NotificationRule>> GetActiveRulesAsync(CancellationToken cancellationToken = default);
    Task<List<NotificationRule>> GetRulesByRoleAsync(UserRole role, CancellationToken cancellationToken = default);
    Task<List<NotificationRule>> GetRulesByTypeAsync(string type, CancellationToken cancellationToken = default);
}


