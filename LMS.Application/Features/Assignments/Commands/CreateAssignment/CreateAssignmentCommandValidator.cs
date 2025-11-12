using FluentValidation;
using LMS.Application.Features.Assignments.Commands.CreateAssignment;

namespace LMS.Application.Features.Assignments.Commands.CreateAssignment;

public class CreateAssignmentCommandValidator : AbstractValidator<CreateAssignmentCommand>
{
    public CreateAssignmentCommandValidator()
    {
        RuleFor(x => x.CourseId)
            .GreaterThan(0).WithMessage("CourseId must be a valid course ID.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(300).WithMessage("Title must not exceed 300 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Type must be a valid AssignmentType.");

        RuleFor(x => x.Deadline)
            .NotEmpty().WithMessage("Deadline is required.")
            .Must(deadline => deadline > DateTime.UtcNow)
            .WithMessage("Deadline must be in the future.");

        RuleFor(x => x.CreatedBy)
            .GreaterThan(0).WithMessage("CreatedBy must be a valid user ID.");
    }
}

