using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LMS.Infrastructure.Repositories;

public class RedeemableItemRepository : EfRepository<RedeemableItem>, IRedeemableItemRepository
{
    public RedeemableItemRepository(LmsDbContext context) : base(context)
    {
    }

    public async Task<List<RedeemableItem>> GetActiveItemsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(item => item.IsActive)
            .OrderBy(item => item.CostPoints)
            .ToListAsync(cancellationToken);
    }
}


