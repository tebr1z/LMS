using FluentValidation;

namespace LMS.Application.Features.Assignments.Commands.PublishAssignment;

public class PublishAssignmentCommandValidator : AbstractValidator<PublishAssignmentCommand>
{
    public PublishAssignmentCommandValidator()
    {
        RuleFor(x => x.AssignmentId)
            .GreaterThan(0).WithMessage("AssignmentId must be a valid assignment ID.");

        RuleFor(x => x.PublishedById)
            .GreaterThan(0).WithMessage("PublishedById must be a valid user ID.");
    }
}

