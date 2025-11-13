using FluentValidation;

namespace LMS.Application.Features.Finance.Commands.AcceptManualPayment;

public class AcceptManualPaymentCommandValidator : AbstractValidator<AcceptManualPaymentCommand>
{
    public AcceptManualPaymentCommandValidator()
    {
        RuleFor(x => x.PaymentId)
            .GreaterThan(0).WithMessage("PaymentId must be a valid payment ID.");

        RuleFor(x => x.Reference)
            .MaximumLength(200).WithMessage("Reference must not exceed 200 characters.")
            .When(x => !string.IsNullOrEmpty(x.Reference));

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Notes must not exceed 1000 characters.")
            .When(x => !string.IsNullOrEmpty(x.Notes));

        RuleFor(x => x.AcceptedById)
            .GreaterThan(0).WithMessage("AcceptedById must be a valid user ID.");
    }
}

