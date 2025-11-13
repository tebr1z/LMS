namespace LMS.Domain.Entities;

public class SystemSettings : BaseEntity
{
    public string Key { get; set; } = string.Empty; // Unique setting key
    public string Value { get; set; } = string.Empty; // Setting value (stored as string, parsed as needed)
    public string? Description { get; set; } // Description of the setting
    public string Category { get; set; } = "General"; // Category: "PassThresholds", "Leaderboard", etc.
}

