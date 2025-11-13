using MediatR;

namespace LMS.Application.Features.Assignments.Commands.SubmitAssignment;

public class SubmitAssignmentCommand : IRequest<int>
{
    public int AssignmentId { get; set; }
    public int StudentId { get; set; }
    public string? FileUrl { get; set; }
    public string? AnswerText { get; set; }
    public int? TimeOnPageInSeconds { get; set; } // Time student spent on assignment page
}

