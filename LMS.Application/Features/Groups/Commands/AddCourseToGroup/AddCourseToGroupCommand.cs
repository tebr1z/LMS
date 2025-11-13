using MediatR;

namespace LMS.Application.Features.Groups.Commands.AddCourseToGroup;

public class AddCourseToGroupCommand : IRequest<int>
{
    public int GroupId { get; set; }
    public int CoursePreparedId { get; set; }
    public int AddedBy { get; set; } // User who is adding (for authorization check)
    public bool CopyAssignmentsFlag { get; set; } = false; // If true, copy template assignments metadata
}

