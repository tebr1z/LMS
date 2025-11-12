using FluentValidation;

namespace LMS.Application.Features.Enrollments.Commands.EnrollInCourse;

public class EnrollInCourseCommandValidator : AbstractValidator<EnrollInCourseCommand>
{
    public EnrollInCourseCommandValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0).WithMessage("UserId must be a valid user ID.");

        RuleFor(x => x.CourseId)
            .GreaterThan(0).WithMessage("CourseId must be a valid course ID.");
    }
}

