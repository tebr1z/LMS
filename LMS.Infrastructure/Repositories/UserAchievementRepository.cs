using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LMS.Infrastructure.Repositories;

public class UserAchievementRepository : EfRepository<UserAchievement>, IUserAchievementRepository
{
    public UserAchievementRepository(LmsDbContext context) : base(context)
    {
    }

    public async Task<List<UserAchievement>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(ua => ua.Achievement)
            .Where(ua => ua.UserId == userId)
            .OrderByDescending(ua => ua.EarnedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasAchievementAsync(int userId, int achievementId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AnyAsync(ua => ua.UserId == userId && ua.AchievementId == achievementId, cancellationToken);
    }

    public async Task<UserAchievement?> GetByUserAndAchievementAsync(int userId, int achievementId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(ua => ua.Achievement)
            .FirstOrDefaultAsync(ua => ua.UserId == userId && ua.AchievementId == achievementId, cancellationToken);
    }
}

