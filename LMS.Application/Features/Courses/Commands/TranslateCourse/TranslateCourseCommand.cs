using MediatR;

namespace LMS.Application.Features.Courses.Commands.TranslateCourse;

public class TranslateCourseCommand : IRequest<TranslateCourseResponse>
{
    public int CourseId { get; set; }
    public string LangCode { get; set; } = string.Empty; // Target language code (e.g., "tr", "ru", "az")
}

public class TranslateCourseResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int CourseId { get; set; }
    public string LangCode { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? ErrorMessage { get; set; }
}

