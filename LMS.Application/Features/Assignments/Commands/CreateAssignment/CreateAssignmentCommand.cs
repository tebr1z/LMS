using LMS.Domain.Enums;
using MediatR;

namespace LMS.Application.Features.Assignments.Commands.CreateAssignment;

public class CreateAssignmentCommand : IRequest<int>
{
    public int CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public AssignmentType Type { get; set; }
    public DateTime Deadline { get; set; }
    public int CreatedBy { get; set; }
}

