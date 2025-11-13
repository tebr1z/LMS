namespace LMS.Domain.Entities;

/// <summary>
/// Localized content for courses (multilingual support)
/// </summary>
public class CourseLocalized : BaseEntity
{
    public int CourseId { get; set; }
    public string LangCode { get; set; } = string.Empty; // e.g., "en", "tr", "az", "ru"
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ContentUrlLocalized { get; set; } // URL to translated content file

    // Navigation property
    public virtual Course Course { get; set; } = null!;
}

