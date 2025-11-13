using LMS.Domain.Entities;

namespace LMS.Application.Interfaces;

public interface IAchievementRepository : IRepository<Achievement>
{
    Task<List<Achievement>> GetActiveAchievementsAsync(CancellationToken cancellationToken = default);
}

