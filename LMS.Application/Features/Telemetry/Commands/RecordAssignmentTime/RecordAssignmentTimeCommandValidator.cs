using FluentValidation;

namespace LMS.Application.Features.Telemetry.Commands.RecordAssignmentTime;

public class RecordAssignmentTimeCommandValidator : AbstractValidator<RecordAssignmentTimeCommand>
{
    public RecordAssignmentTimeCommandValidator()
    {
        RuleFor(x => x.StudentId)
            .GreaterThan(0).WithMessage("StudentId must be a valid student ID.");

        RuleFor(x => x.AssignmentId)
            .GreaterThan(0).WithMessage("AssignmentId must be a valid assignment ID.");

        RuleFor(x => x.SecondsActive)
            .GreaterThanOrEqualTo(0).WithMessage("SecondsActive must be greater than or equal to 0.")
            .LessThanOrEqualTo(3600).WithMessage("SecondsActive should not exceed 3600 seconds (1 hour) per event.");

        RuleFor(x => x.SessionId)
            .NotEmpty().WithMessage("SessionId is required.")
            .MaximumLength(100).WithMessage("SessionId must not exceed 100 characters.");
    }
}

