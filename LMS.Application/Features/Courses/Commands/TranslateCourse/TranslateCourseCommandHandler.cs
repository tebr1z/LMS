using LMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LMS.Application.Features.Courses.Commands.TranslateCourse;

public class TranslateCourseCommandHandler : IRequestHandler<TranslateCourseCommand, TranslateCourseResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITranslationService _translationService;
    private readonly ILogger<TranslateCourseCommandHandler> _logger;

    public TranslateCourseCommandHandler(
        IUnitOfWork unitOfWork,
        ITranslationService translationService,
        ILogger<TranslateCourseCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _translationService = translationService;
        _logger = logger;
    }

    public async Task<TranslateCourseResponse> Handle(TranslateCourseCommand request, CancellationToken cancellationToken)
    {
        // Validate course exists
        var course = await _unitOfWork.Courses.GetByIdAsync(request.CourseId);
        if (course == null)
        {
            return new TranslateCourseResponse
            {
                Success = false,
                Message = "Course not found.",
                CourseId = request.CourseId,
                LangCode = request.LangCode,
                ErrorMessage = $"Course with ID {request.CourseId} not found."
            };
        }

        // Normalize language code
        var langCode = request.LangCode.ToLowerInvariant();

        try
        {
            // Translate the course
            var result = await _translationService.TranslateCourseAsync(request.CourseId, langCode, cancellationToken);

            if (result.Success)
            {
                return new TranslateCourseResponse
                {
                    Success = true,
                    Message = $"Course successfully translated to {langCode}.",
                    CourseId = result.CourseId,
                    LangCode = result.LangCode,
                    Title = result.Title,
                    Description = result.Description
                };
            }
            else
            {
                return new TranslateCourseResponse
                {
                    Success = false,
                    Message = "Translation failed.",
                    CourseId = request.CourseId,
                    LangCode = langCode,
                    ErrorMessage = result.ErrorMessage
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error translating course {CourseId} to {LangCode}", request.CourseId, langCode);
            return new TranslateCourseResponse
            {
                Success = false,
                Message = "An error occurred during translation.",
                CourseId = request.CourseId,
                LangCode = langCode,
                ErrorMessage = ex.Message
            };
        }
    }
}

