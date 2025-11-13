namespace LMS.Domain.Entities;

/// <summary>
/// RewardPoint entity for tracking user points
/// </summary>
public class RewardPoint : BaseEntity
{
    public int UserId { get; set; }
    public int Points { get; set; } // Can be positive (earned) or negative (spent)
    public string Reason { get; set; } = string.Empty; // e.g., "Achievement earned: First Assignment", "Redeemed: Extra Credit"
    // Note: CreatedAt is inherited from BaseEntity
}

