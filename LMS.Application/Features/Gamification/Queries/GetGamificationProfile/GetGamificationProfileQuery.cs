using MediatR;

namespace LMS.Application.Features.Gamification.Queries.GetGamificationProfile;

public class GetGamificationProfileQuery : IRequest<GamificationProfileDto>
{
    public int UserId { get; set; }
}

public class GamificationProfileDto
{
    public int UserId { get; set; }
    public int TotalPoints { get; set; }
    public List<AchievementDto> Achievements { get; set; } = new();
    public List<RewardPointDto> RecentPoints { get; set; } = new();
}

public class AchievementDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? IconUrl { get; set; }
    public int PointsReward { get; set; }
    public DateTime EarnedAt { get; set; }
}

public class RewardPointDto
{
    public int Id { get; set; }
    public int Points { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

