using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LMS.Application.Features.Courses.Commands.CreateCourse;

public class CreateCourseCommandHandler : IRequestHandler<CreateCourseCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITranslationService _translationService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<CreateCourseCommandHandler> _logger;

    public CreateCourseCommandHandler(
        IUnitOfWork unitOfWork,
        ITranslationService translationService,
        IConfiguration configuration,
        ILogger<CreateCourseCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _translationService = translationService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<int> Handle(CreateCourseCommand request, CancellationToken cancellationToken)
    {
        var course = new Course
        {
            Title = request.Title,
            Description = request.Description,
            CreatedBy = request.CreatedBy,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Courses.AddAsync(course);
        await _unitOfWork.SaveChangesAsync();

        // Auto-translate if enabled (fire and forget - don't block course creation)
        var autoTranslateOnCreate = _configuration.GetValue<bool>("Translation:AutoTranslateOnCreate", true);
        if (autoTranslateOnCreate)
        {
            try
            {
                // Use background task - don't await to avoid blocking course creation
                // Translation will happen asynchronously in the background
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // Create a new scope for the background task since we're outside the request scope
                        await Task.Delay(1000, cancellationToken); // Small delay to ensure course is saved
                        
                        // Note: This requires IServiceScopeFactory if used in production
                        // For now, translation service should handle its own scope or use IHttpClientFactory
                        await _translationService.AutoTranslateCourseAsync(course.Id, CancellationToken.None);
                        _logger.LogInformation("Auto-translated course {CourseId} to all supported languages", course.Id);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error auto-translating course {CourseId}", course.Id);
                    }
                }, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to start auto-translation for course {CourseId}", course.Id);
                // Don't fail course creation if translation setup fails
            }
        }

        return course.Id;
    }
}

