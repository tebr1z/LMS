using FluentValidation;

namespace LMS.Application.Features.StudentNotes.Commands.AddStudentNote;

public class AddStudentNoteCommandValidator : AbstractValidator<AddStudentNoteCommand>
{
    public AddStudentNoteCommandValidator()
    {
        RuleFor(x => x.StudentId)
            .GreaterThan(0).WithMessage("StudentId must be a valid student ID.");

        RuleFor(x => x.GroupId)
            .GreaterThan(0).WithMessage("GroupId must be a valid group ID.");

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Content is required.")
            .MaximumLength(2000).WithMessage("Content must not exceed 2000 characters.");

        RuleFor(x => x.CreatedById)
            .GreaterThan(0).WithMessage("CreatedById must be a valid user ID.");
    }
}

