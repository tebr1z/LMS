namespace LMS.Domain.Entities;

/// <summary>
/// Learning level entity for tracking student difficulty level
/// </summary>
public class LearningLevel : BaseEntity
{
    public int StudentId { get; set; }
    public int DifficultyLevel { get; set; } = 2; // 1 = Easy, 2 = Medium, 3 = Hard
    // Note: UpdatedAt is inherited from BaseEntity
}

