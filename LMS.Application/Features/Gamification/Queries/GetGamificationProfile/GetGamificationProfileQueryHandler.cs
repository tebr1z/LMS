using LMS.Application.Interfaces;
using MediatR;

namespace LMS.Application.Features.Gamification.Queries.GetGamificationProfile;

public class GetGamificationProfileQueryHandler : IRequestHandler<GetGamificationProfileQuery, GamificationProfileDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetGamificationProfileQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GamificationProfileDto> Handle(GetGamificationProfileQuery request, CancellationToken cancellationToken)
    {
        // Get total points
        var totalPoints = await _unitOfWork.RewardPoints.GetTotalPointsAsync(request.UserId, cancellationToken);

        // Get achievements
        var userAchievements = await _unitOfWork.UserAchievements.GetByUserIdAsync(request.UserId, cancellationToken);
        var achievements = userAchievements.Select(ua => new AchievementDto
        {
            Id = ua.Achievement.Id,
            Name = ua.Achievement.Name,
            Description = ua.Achievement.Description,
            IconUrl = ua.Achievement.IconUrl,
            PointsReward = ua.Achievement.PointsReward,
            EarnedAt = ua.EarnedAt
        }).ToList();

        // Get recent points (last 20)
        var allPoints = await _unitOfWork.RewardPoints.GetByUserIdAsync(request.UserId, cancellationToken);
        var recentPoints = allPoints
            .OrderByDescending(rp => rp.CreatedAt)
            .Take(20)
            .Select(rp => new RewardPointDto
            {
                Id = rp.Id,
                Points = rp.Points,
                Reason = rp.Reason,
                CreatedAt = rp.CreatedAt
            })
            .ToList();

        return new GamificationProfileDto
        {
            UserId = request.UserId,
            TotalPoints = totalPoints,
            Achievements = achievements,
            RecentPoints = recentPoints
        };
    }
}

