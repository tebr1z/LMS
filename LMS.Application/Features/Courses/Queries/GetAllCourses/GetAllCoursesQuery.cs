using LMS.Application.DTOs.Courses;
using MediatR;

namespace LMS.Application.Features.Courses.Queries.GetAllCourses;

public class GetAllCoursesQuery : IRequest<IEnumerable<CourseDto>>
{
}

