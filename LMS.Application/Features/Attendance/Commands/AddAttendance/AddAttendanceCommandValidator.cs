using FluentValidation;

namespace LMS.Application.Features.Attendance.Commands.AddAttendance;

public class AddAttendanceCommandValidator : AbstractValidator<AddAttendanceCommand>
{
    public AddAttendanceCommandValidator()
    {
        RuleFor(x => x.GroupId)
            .GreaterThan(0).WithMessage("GroupId must be a valid group ID.");

        RuleFor(x => x.StudentId)
            .GreaterThan(0).WithMessage("StudentId must be a valid student ID.");

        RuleFor(x => x.Date)
            .NotEmpty().WithMessage("Date is required.")
            .Must(date => date.Date <= DateTime.UtcNow.Date.AddDays(1))
            .WithMessage("Date cannot be more than one day in the future.");

        RuleFor(x => x.Notes)
            .MaximumLength(500).WithMessage("Notes must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.Notes));

        RuleFor(x => x.MarkedById)
            .GreaterThan(0).WithMessage("MarkedById must be a valid user ID.");
    }
}

