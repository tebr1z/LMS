using LMS.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LMS.Application.Features.Courses.Queries.GetCourseById;

public class GetCourseByIdQueryHandler : IRequestHandler<GetCourseByIdQuery, CourseLocalizedDto?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GetCourseByIdQueryHandler> _logger;

    public GetCourseByIdQueryHandler(
        IUnitOfWork unitOfWork,
        IUserRepository userRepository,
        ILogger<GetCourseByIdQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<CourseLocalizedDto?> Handle(GetCourseByIdQuery request, CancellationToken cancellationToken)
    {
        var course = await _unitOfWork.Courses.GetByIdAsync(request.CourseId);
        if (course == null)
        {
            return null;
        }

        // If language code is provided, try to get localized version
        if (!string.IsNullOrWhiteSpace(request.LangCode))
        {
            var langCode = request.LangCode.ToLowerInvariant();
            
            // Check if localized version exists
            var localized = await _unitOfWork.CourseLocalized.GetByCourseIdAndLangAsync(request.CourseId, langCode, cancellationToken);
            
            if (localized != null)
            {
                // Return localized version
                var creatorName = await _userRepository.GetUserFullNameAsync(course.CreatedBy);
                
                return new CourseLocalizedDto
                {
                    Id = course.Id,
                    Title = localized.Title,
                    Description = localized.Description,
                    ContentUrl = null, // Original content URL not in localized entity
                    ContentUrlLocalized = localized.ContentUrlLocalized,
                    LangCode = localized.LangCode,
                    CreatedBy = course.CreatedBy,
                    CreatorName = creatorName ?? "Unknown",
                    CreatedAt = course.CreatedAt,
                    UpdatedAt = localized.UpdatedAt
                };
            }
            
            // If localized version doesn't exist, fall back to original (English/default)
            _logger.LogInformation("Localized version not found for course {CourseId} in language {LangCode}, returning original", request.CourseId, langCode);
        }

        // Return original course (default/English version)
        var originalCreatorName = await _userRepository.GetUserFullNameAsync(course.CreatedBy);
        
        return new CourseLocalizedDto
        {
            Id = course.Id,
            Title = course.Title,
            Description = course.Description,
            ContentUrl = null, // Course entity doesn't have ContentUrl, might be in CoursePrepared
            ContentUrlLocalized = null,
            LangCode = "en", // Default language
            CreatedBy = course.CreatedBy,
            CreatorName = originalCreatorName ?? "Unknown",
            CreatedAt = course.CreatedAt,
            UpdatedAt = course.UpdatedAt
        };
    }
}

