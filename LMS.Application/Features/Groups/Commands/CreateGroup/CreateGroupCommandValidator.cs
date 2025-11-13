using FluentValidation;

namespace LMS.Application.Features.Groups.Commands.CreateGroup;

public class CreateGroupCommandValidator : AbstractValidator<CreateGroupCommand>
{
    public CreateGroupCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Group title is required.")
            .MaximumLength(200).WithMessage("Group title must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.");

        RuleFor(x => x.CreatedById)
            .GreaterThan(0).WithMessage("CreatedById must be a valid user ID.");
    }
}

