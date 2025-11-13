using FluentValidation;

namespace LMS.Application.Features.Assignments.Commands.EditAssignment;

public class EditAssignmentCommandValidator : AbstractValidator<EditAssignmentCommand>
{
    public EditAssignmentCommandValidator()
    {
        RuleFor(x => x.AssignmentId)
            .GreaterThan(0).WithMessage("AssignmentId must be a valid assignment ID.");

        RuleFor(x => x.Title)
            .MaximumLength(300).WithMessage("Title must not exceed 300 characters.")
            .When(x => x.Title != null);

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.")
            .When(x => x.Description != null);

        RuleFor(x => x.MaxScore)
            .GreaterThanOrEqualTo(0).WithMessage("MaxScore must be greater than or equal to 0.")
            .LessThanOrEqualTo(1000).WithMessage("MaxScore must be less than or equal to 1000.")
            .When(x => x.MaxScore.HasValue);

        RuleFor(x => x.Deadline)
            .Must(deadline => !deadline.HasValue || deadline.Value > DateTime.UtcNow)
            .WithMessage("Deadline must be in the future if provided.")
            .When(x => x.Deadline.HasValue);

        RuleFor(x => x.EditedById)
            .GreaterThan(0).WithMessage("EditedById must be a valid user ID.");
    }
}

