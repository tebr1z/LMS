using LMS.Domain.Entities;

namespace LMS.Application.Interfaces;

public interface IUserAchievementRepository : IRepository<UserAchievement>
{
    Task<List<UserAchievement>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<bool> HasAchievementAsync(int userId, int achievementId, CancellationToken cancellationToken = default);
    Task<UserAchievement?> GetByUserAndAchievementAsync(int userId, int achievementId, CancellationToken cancellationToken = default);
}


