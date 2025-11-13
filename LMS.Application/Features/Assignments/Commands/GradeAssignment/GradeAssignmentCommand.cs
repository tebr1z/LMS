using MediatR;

namespace LMS.Application.Features.Assignments.Commands.GradeAssignment;

public class GradeAssignmentCommand : IRequest<bool>
{
    public int SubmissionId { get; set; }
    public int Score { get; set; } // Score (0..MaxScore)
    public string? Feedback { get; set; }
    public int EvaluatedById { get; set; }
}

