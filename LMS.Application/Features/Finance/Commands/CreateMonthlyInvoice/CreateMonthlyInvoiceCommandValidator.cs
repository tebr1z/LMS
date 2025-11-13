using FluentValidation;

namespace LMS.Application.Features.Finance.Commands.CreateMonthlyInvoice;

public class CreateMonthlyInvoiceCommandValidator : AbstractValidator<CreateMonthlyInvoiceCommand>
{
    public CreateMonthlyInvoiceCommandValidator()
    {
        RuleFor(x => x.StudentId)
            .GreaterThan(0).WithMessage("StudentId must be a valid student ID.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than 0.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .MaximumLength(10).WithMessage("Currency must not exceed 10 characters.");

        RuleFor(x => x.DueDate)
            .NotEmpty().WithMessage("DueDate is required.")
            .Must(date => date >= DateTime.UtcNow.Date)
            .WithMessage("DueDate must be today or in the future.");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Description must not exceed 500 characters.")
            .When(x => !string.IsNullOrEmpty(x.Description));

        RuleFor(x => x.CreatedById)
            .GreaterThan(0).WithMessage("CreatedById must be a valid user ID.");
    }
}

