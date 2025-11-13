using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace LMS.Infrastructure.Services;

/// <summary>
/// Service implementation for translation using OpenAI API
/// </summary>
public class TranslationService : ITranslationService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TranslationService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _model;
    private readonly List<string> _supportedLanguages;

    public TranslationService(
        IUnitOfWork unitOfWork,
        IConfiguration configuration,
        ILogger<TranslationService> logger,
        HttpClient httpClient)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient; // HttpClient is configured in DependencyInjection

        _model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";

        // Get supported languages from configuration (default: az, tr, en, ru)
        var languagesConfig = _configuration["Translation:SupportedLanguages"] ?? "az,tr,en,ru";
        _supportedLanguages = languagesConfig.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(l => l.ToLowerInvariant())
            .ToList();
    }

    public async Task<string> TranslateTextAsync(string text, string targetLangCode, string sourceLangCode = "en", CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        // Get language names for better prompts
        var targetLangName = GetLanguageName(targetLangCode);
        var sourceLangName = GetLanguageName(sourceLangCode);

        try
        {
            // Prepare the prompt
            var prompt = $"Translate the following text from {sourceLangName} to {targetLangName}. Only return the translation, nothing else.\n\nText to translate:\n{text}";

            // Prepare the request body
            var requestBody = new
            {
                model = _model,
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = prompt
                    }
                },
                max_tokens = 2000,
                temperature = 0.3 // Lower temperature for more consistent translations
            };

            // Send request to OpenAI API
            var response = await _httpClient.PostAsJsonAsync(
                "chat/completions",
                requestBody,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var jsonResponse = JsonDocument.Parse(responseContent);

            // Extract the translation
            var translation = jsonResponse.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? text;

            return translation.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error translating text from {SourceLang} to {TargetLang}", sourceLangCode, targetLangCode);
            throw;
        }
    }

    public async Task<CourseTranslationResult> TranslateCourseAsync(int courseId, string targetLangCode, CancellationToken cancellationToken = default)
    {
        try
        {
            // Get the original course
            var course = await _unitOfWork.Courses.GetByIdAsync(courseId);
            if (course == null)
            {
                return new CourseTranslationResult
                {
                    CourseId = courseId,
                    LangCode = targetLangCode,
                    Success = false,
                    ErrorMessage = $"Course with ID {courseId} not found."
                };
            }

            // Check if translation already exists
            var existingTranslation = await _unitOfWork.CourseLocalized.GetByCourseIdAndLangAsync(courseId, targetLangCode);
            if (existingTranslation != null)
            {
                _logger.LogInformation("Translation already exists for course {CourseId} in language {LangCode}", courseId, targetLangCode);
                return new CourseTranslationResult
                {
                    CourseId = courseId,
                    LangCode = targetLangCode,
                    Title = existingTranslation.Title,
                    Description = existingTranslation.Description,
                    Success = true
                };
            }

            // Determine source language (default: English, or detect from course title/description)
            var sourceLangCode = "en"; // Default to English

            // Translate title
            var translatedTitle = await TranslateTextAsync(course.Title, targetLangCode, sourceLangCode, cancellationToken);

            // Translate description if available
            string? translatedDescription = null;
            if (!string.IsNullOrWhiteSpace(course.Description))
            {
                translatedDescription = await TranslateTextAsync(course.Description, targetLangCode, sourceLangCode, cancellationToken);
            }

            // Create localized course entry
            var courseLocalized = new CourseLocalized
            {
                CourseId = courseId,
                LangCode = targetLangCode.ToLowerInvariant(),
                Title = translatedTitle,
                Description = translatedDescription,
                ContentUrlLocalized = null, // File content translation would require file parsing/translation
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.CourseLocalized.AddAsync(courseLocalized);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Translated course {CourseId} to language {LangCode}",
                courseId, targetLangCode);

            return new CourseTranslationResult
            {
                CourseId = courseId,
                LangCode = targetLangCode,
                Title = translatedTitle,
                Description = translatedDescription,
                Success = true
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error translating course {CourseId} to {LangCode}", courseId, targetLangCode);
            return new CourseTranslationResult
            {
                CourseId = courseId,
                LangCode = targetLangCode,
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    public async Task AutoTranslateCourseAsync(int courseId, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting auto-translation for course {CourseId} to all supported languages", courseId);

            var tasks = _supportedLanguages
                .Where(lang => lang != "en") // Skip English (default language)
                .Select(lang => TranslateCourseAsync(courseId, lang, cancellationToken));

            var results = await Task.WhenAll(tasks);

            var successCount = results.Count(r => r.Success);
            var errorCount = results.Count(r => !r.Success);

            _logger.LogInformation(
                "Completed auto-translation for course {CourseId}. Success: {Success}, Errors: {Errors}",
                courseId, successCount, errorCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in auto-translation for course {CourseId}", courseId);
            throw;
        }
    }

    private string GetLanguageName(string langCode)
    {
        return langCode.ToLowerInvariant() switch
        {
            "en" => "English",
            "tr" => "Turkish",
            "az" => "Azerbaijani",
            "ru" => "Russian",
            "fr" => "French",
            "de" => "German",
            "es" => "Spanish",
            "it" => "Italian",
            "pt" => "Portuguese",
            "ar" => "Arabic",
            "zh" => "Chinese",
            "ja" => "Japanese",
            "ko" => "Korean",
            _ => langCode // Fallback to code if unknown
        };
    }
}

