namespace LMS.Domain.Entities;

/// <summary>
/// UserAchievement entity linking users to achievements they've earned
/// </summary>
public class UserAchievement : BaseEntity
{
    public int UserId { get; set; }
    public int AchievementId { get; set; }
    public DateTime EarnedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Achievement Achievement { get; set; } = null!;
}


