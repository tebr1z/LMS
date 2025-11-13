using FluentValidation;

namespace LMS.Application.Features.StudentOffice.Commands.FlagStudent;

public class FlagStudentCommandValidator : AbstractValidator<FlagStudentCommand>
{
    public FlagStudentCommandValidator()
    {
        RuleFor(x => x.StudentId)
            .GreaterThan(0).WithMessage("StudentId must be a valid student ID.");

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage("Reason is required.")
            .MaximumLength(500).WithMessage("Reason must not exceed 500 characters.");

        RuleFor(x => x.RecommendedAction)
            .NotEmpty().WithMessage("RecommendedAction is required.")
            .MaximumLength(1000).WithMessage("RecommendedAction must not exceed 1000 characters.");

        RuleFor(x => x.CreatedById)
            .GreaterThan(0).WithMessage("CreatedById must be a valid user ID.");
    }
}

