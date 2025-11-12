using FluentValidation;

namespace LMS.Application.Features.Assignments.Commands.SubmitAssignment;

public class SubmitAssignmentCommandValidator : AbstractValidator<SubmitAssignmentCommand>
{
    public SubmitAssignmentCommandValidator()
    {
        RuleFor(x => x.AssignmentId)
            .GreaterThan(0).WithMessage("AssignmentId must be a valid assignment ID.");

        RuleFor(x => x.StudentId)
            .GreaterThan(0).WithMessage("StudentId must be a valid user ID.");

        // At least one of FileUrl or AnswerText must be provided
        RuleFor(x => x)
            .Must(x => !string.IsNullOrEmpty(x.FileUrl) || !string.IsNullOrEmpty(x.AnswerText))
            .WithMessage("Either FileUrl or AnswerText must be provided.");

        RuleFor(x => x.FileUrl)
            .MaximumLength(500).WithMessage("FileUrl must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.FileUrl));

        RuleFor(x => x.AnswerText)
            .MaximumLength(5000).WithMessage("AnswerText must not exceed 5000 characters.")
            .When(x => !string.IsNullOrEmpty(x.AnswerText));
    }
}

