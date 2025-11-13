using LMS.Domain.Entities;

namespace LMS.Application.Interfaces;

public interface IRewardPointRepository : IRepository<RewardPoint>
{
    Task<List<RewardPoint>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
    Task<int> GetTotalPointsAsync(int userId, CancellationToken cancellationToken = default);
}


