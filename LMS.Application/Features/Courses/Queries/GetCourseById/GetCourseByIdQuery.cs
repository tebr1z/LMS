using MediatR;

namespace LMS.Application.Features.Courses.Queries.GetCourseById;

public class GetCourseByIdQuery : IRequest<CourseLocalizedDto?>
{
    public int CourseId { get; set; }
    public string? LangCode { get; set; } // Optional language code (e.g., "tr", "ru", "az")
}

public class CourseLocalizedDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ContentUrl { get; set; }
    public string? ContentUrlLocalized { get; set; }
    public string LangCode { get; set; } = "en"; // Language of the returned content
    public int CreatedBy { get; set; }
    public string? CreatorName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

