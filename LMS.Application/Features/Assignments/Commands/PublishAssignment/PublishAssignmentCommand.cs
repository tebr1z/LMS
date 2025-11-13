using MediatR;

namespace LMS.Application.Features.Assignments.Commands.PublishAssignment;

public class PublishAssignmentCommand : IRequest<bool>
{
    public int AssignmentId { get; set; }
    public int PublishedById { get; set; }
}

