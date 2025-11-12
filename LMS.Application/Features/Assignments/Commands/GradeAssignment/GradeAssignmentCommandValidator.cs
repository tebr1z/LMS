using FluentValidation;

namespace LMS.Application.Features.Assignments.Commands.GradeAssignment;

public class GradeAssignmentCommandValidator : AbstractValidator<GradeAssignmentCommand>
{
    public GradeAssignmentCommandValidator()
    {
        RuleFor(x => x.AssignmentId)
            .GreaterThan(0).WithMessage("AssignmentId must be a valid assignment ID.");

        RuleFor(x => x.SubmissionId)
            .GreaterThan(0).WithMessage("SubmissionId must be a valid submission ID.");

        RuleFor(x => x.Score)
            .GreaterThanOrEqualTo(0).WithMessage("Score must be between 0 and 100.")
            .LessThanOrEqualTo(100).WithMessage("Score must be between 0 and 100.");

        RuleFor(x => x.EvaluatedBy)
            .GreaterThan(0).WithMessage("EvaluatedBy must be a valid user ID.");
    }
}

