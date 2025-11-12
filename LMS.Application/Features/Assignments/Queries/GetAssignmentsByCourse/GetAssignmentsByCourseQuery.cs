using LMS.Application.DTOs.Assignments;
using MediatR;

namespace LMS.Application.Features.Assignments.Queries.GetAssignmentsByCourse;

public class GetAssignmentsByCourseQuery : IRequest<IEnumerable<AssignmentDto>>
{
    public int CourseId { get; set; }
}

