using FluentValidation;
using LMS.Application.Features.Assignments.Commands.CreateAssignment;
using LMS.Domain.Enums;

namespace LMS.Application.Features.Assignments.Commands.CreateAssignment;

public class CreateAssignmentCommandValidator : AbstractValidator<CreateAssignmentCommand>
{
    public CreateAssignmentCommandValidator()
    {
        RuleFor(x => x.CourseInstanceId)
            .GreaterThan(0).WithMessage("CourseInstanceId must be a valid course instance ID.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(300).WithMessage("Title must not exceed 300 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(2000).WithMessage("Description must not exceed 2000 characters.");

        RuleFor(x => x.AssignmentType)
            .IsInEnum().WithMessage("AssignmentType must be a valid AssignmentType.");

        RuleFor(x => x.MaxScore)
            .GreaterThanOrEqualTo(0).WithMessage("MaxScore must be greater than or equal to 0.")
            .LessThanOrEqualTo(1000).WithMessage("MaxScore must be less than or equal to 1000.");

        RuleFor(x => x.Deadline)
            .Must(deadline => !deadline.HasValue || deadline.Value > DateTime.UtcNow)
            .WithMessage("Deadline must be in the future if provided.");

        RuleFor(x => x.CreatedById)
            .GreaterThan(0).WithMessage("CreatedById must be a valid user ID.");
    }
}

