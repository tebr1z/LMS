using MediatR;

namespace LMS.Application.Features.Assignments.Commands.GradeAssignment;

public class GradeAssignmentCommand : IRequest<bool>
{
    public int AssignmentId { get; set; }
    public int SubmissionId { get; set; }
    public decimal Score { get; set; }
    public int EvaluatedBy { get; set; }
}

