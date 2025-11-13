namespace LMS.Domain.Entities;

/// <summary>
/// RedeemableItem entity for items that can be purchased with points
/// </summary>
public class RedeemableItem : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int CostPoints { get; set; }
    public string BenefitType { get; set; } = string.Empty; // e.g., "ExtraCredit", "SkipAssignment", "Certificate"
    public string? BenefitValue { get; set; } // JSON string for benefit-specific data
    public bool IsActive { get; set; } = true;
}


