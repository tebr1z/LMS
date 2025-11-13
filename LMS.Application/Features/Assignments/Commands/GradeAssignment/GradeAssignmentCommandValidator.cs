using FluentValidation;

namespace LMS.Application.Features.Assignments.Commands.GradeAssignment;

public class GradeAssignmentCommandValidator : AbstractValidator<GradeAssignmentCommand>
{
    public GradeAssignmentCommandValidator()
    {
        RuleFor(x => x.SubmissionId)
            .GreaterThan(0).WithMessage("SubmissionId must be a valid submission ID.");

        RuleFor(x => x.Score)
            .GreaterThanOrEqualTo(0).WithMessage("Score must be greater than or equal to 0.");

        RuleFor(x => x.Feedback)
            .MaximumLength(2000).WithMessage("Feedback must not exceed 2000 characters.")
            .When(x => x.Feedback != null);

        RuleFor(x => x.EvaluatedById)
            .GreaterThan(0).WithMessage("EvaluatedById must be a valid user ID.");
    }
}

