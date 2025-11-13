using MediatR;

namespace LMS.Application.Features.Assignments.Commands.EditAssignment;

public class EditAssignmentCommand : IRequest<int>
{
    public int AssignmentId { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public int? MaxScore { get; set; }
    public DateTime? Deadline { get; set; }
    public bool? AllowEditAfterPublish { get; set; }
    public bool? AllowResubmit { get; set; }
    public int EditedById { get; set; }
}

