using FluentValidation;

namespace LMS.Application.Features.Groups.Commands.AddCourseToGroup;

public class AddCourseToGroupCommandValidator : AbstractValidator<AddCourseToGroupCommand>
{
    public AddCourseToGroupCommandValidator()
    {
        RuleFor(x => x.GroupId)
            .GreaterThan(0).WithMessage("GroupId must be a valid group ID.");

        RuleFor(x => x.CoursePreparedId)
            .GreaterThan(0).WithMessage("CoursePreparedId must be a valid course prepared ID.");

        RuleFor(x => x.AddedBy)
            .GreaterThan(0).WithMessage("AddedBy must be a valid user ID.");
    }
}

