namespace LMS.Application.Interfaces;

/// <summary>
/// Service interface for translation using OpenAI
/// </summary>
public interface ITranslationService
{
    /// <summary>
    /// Translate text to target language
    /// </summary>
    Task<string> TranslateTextAsync(string text, string targetLangCode, string sourceLangCode = "en", CancellationToken cancellationToken = default);

    /// <summary>
    /// Translate course content to target language
    /// </summary>
    Task<CourseTranslationResult> TranslateCourseAsync(int courseId, string targetLangCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Auto-translate course to all configured languages
    /// </summary>
    Task AutoTranslateCourseAsync(int courseId, CancellationToken cancellationToken = default);
}

public class CourseTranslationResult
{
    public int CourseId { get; set; }
    public string LangCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

