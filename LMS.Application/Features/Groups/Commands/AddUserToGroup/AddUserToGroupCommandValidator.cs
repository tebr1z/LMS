using FluentValidation;

namespace LMS.Application.Features.Groups.Commands.AddUserToGroup;

public class AddUserToGroupCommandValidator : AbstractValidator<AddUserToGroupCommand>
{
    public AddUserToGroupCommandValidator()
    {
        RuleFor(x => x.GroupId)
            .GreaterThan(0).WithMessage("GroupId must be a valid group ID.");

        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("UserId must be a valid user ID.");

        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("Role is required.")
            .MaximumLength(50).WithMessage("Role must not exceed 50 characters.");

        RuleFor(x => x.AddedBy)
            .GreaterThan(0).WithMessage("AddedBy must be a valid user ID.");
    }
}

