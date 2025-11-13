using LMS.Domain.Enums;
using MediatR;

namespace LMS.Application.Features.Assignments.Commands.CreateAssignment;

public class CreateAssignmentCommand : IRequest<int>
{
    public int CourseInstanceId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public AssignmentType AssignmentType { get; set; }
    public int MaxScore { get; set; } = 100;
    public DateTime? Deadline { get; set; } // Nullable deadline
    public bool AllowEditAfterPublish { get; set; } = false;
    public bool AllowResubmit { get; set; } = false;
    public int CreatedById { get; set; }
}

