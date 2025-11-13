namespace LMS.Domain.Entities;

/// <summary>
/// Achievement entity for gamification
/// </summary>
public class Achievement : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? CriteriaJson { get; set; } // JSON string defining achievement criteria
    public string? IconUrl { get; set; }
    public int PointsReward { get; set; } = 0; // Points awarded when achievement is earned

    // Navigation properties
    public virtual ICollection<UserAchievement> UserAchievements { get; set; } = new List<UserAchievement>();
}


