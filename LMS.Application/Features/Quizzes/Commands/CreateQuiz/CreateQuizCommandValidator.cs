using FluentValidation;
using System.Text.Json;

namespace LMS.Application.Features.Quizzes.Commands.CreateQuiz;

public class CreateQuizCommandValidator : AbstractValidator<CreateQuizCommand>
{
    public CreateQuizCommandValidator()
    {
        RuleFor(x => x.AssignmentId)
            .GreaterThan(0).WithMessage("AssignmentId must be a valid assignment ID.");

        RuleFor(x => x.TimeLimitSeconds)
            .GreaterThan(0).WithMessage("TimeLimitSeconds must be greater than 0 if provided.")
            .When(x => x.TimeLimitSeconds.HasValue);

        RuleFor(x => x.PassingThreshold)
            .InclusiveBetween(0, 100).WithMessage("PassingThreshold must be between 0 and 100 if provided.")
            .When(x => x.PassingThreshold.HasValue);

        RuleFor(x => x.Questions)
            .NotEmpty().WithMessage("Quiz must have at least one question.");

        RuleForEach(x => x.Questions)
            .SetValidator(new QuizQuestionDtoValidator());

        RuleFor(x => x.CreatedById)
            .GreaterThan(0).WithMessage("CreatedById must be a valid user ID.");
    }
}

public class QuizQuestionDtoValidator : AbstractValidator<QuizQuestionDto>
{
    public QuizQuestionDtoValidator()
    {
        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Question text is required.")
            .MaximumLength(1000).WithMessage("Question text must not exceed 1000 characters.");

        RuleFor(x => x.Options)
            .NotEmpty().WithMessage("Question must have at least one option.")
            .Must(options => options.Count >= 2).WithMessage("Question must have at least 2 options.");

        RuleFor(x => x.CorrectAnswer)
            .NotEmpty().WithMessage("CorrectAnswer is required.")
            .MaximumLength(500).WithMessage("CorrectAnswer must not exceed 500 characters.");

        RuleFor(x => x.Points)
            .GreaterThan(0).WithMessage("Points must be greater than 0.")
            .LessThanOrEqualTo(100).WithMessage("Points must be less than or equal to 100.");

        RuleFor(x => x.Order)
            .GreaterThanOrEqualTo(0).WithMessage("Order must be greater than or equal to 0.");
    }
}

