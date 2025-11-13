using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LMS.Infrastructure.Repositories;

public class RewardPointRepository : EfRepository<RewardPoint>, IRewardPointRepository
{
    public RewardPointRepository(LmsDbContext context) : base(context)
    {
    }

    public async Task<List<RewardPoint>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(rp => rp.UserId == userId)
            .OrderByDescending(rp => rp.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetTotalPointsAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(rp => rp.UserId == userId)
            .SumAsync(rp => rp.Points, cancellationToken);
    }
}

