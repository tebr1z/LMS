using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LMS.Infrastructure.Repositories;

public class AchievementRepository : EfRepository<Achievement>, IAchievementRepository
{
    public AchievementRepository(LmsDbContext context) : base(context)
    {
    }

    public async Task<List<Achievement>> GetActiveAchievementsAsync(CancellationToken cancellationToken = default)
    {
        // For now, return all achievements. In the future, we might add an IsActive field.
        return await _dbSet.ToListAsync(cancellationToken);
    }
}

