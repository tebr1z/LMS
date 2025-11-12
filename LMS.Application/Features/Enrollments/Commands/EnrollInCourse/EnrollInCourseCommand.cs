using MediatR;

namespace LMS.Application.Features.Enrollments.Commands.EnrollInCourse;

public class EnrollInCourseCommand : IRequest<int>
{
    public int UserId { get; set; }
    public int CourseId { get; set; }
}

