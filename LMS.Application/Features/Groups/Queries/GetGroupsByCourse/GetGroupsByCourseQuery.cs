using LMS.Application.DTOs.Groups;
using MediatR;

namespace LMS.Application.Features.Groups.Queries.GetGroupsByCourse;

public class GetGroupsByCourseQuery : IRequest<IEnumerable<GroupDto>>
{
    public int CourseId { get; set; }
    public int UserId { get; set; }
    public string UserRole { get; set; } = string.Empty;
}

