using LMS.Domain.Entities;

namespace LMS.Application.Interfaces;

public interface IRedeemableItemRepository : IRepository<RedeemableItem>
{
    Task<List<RedeemableItem>> GetActiveItemsAsync(CancellationToken cancellationToken = default);
}


