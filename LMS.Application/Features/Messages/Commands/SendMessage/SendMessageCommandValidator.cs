using FluentValidation;

namespace LMS.Application.Features.Messages.Commands.SendMessage;

public class SendMessageCommandValidator : AbstractValidator<SendMessageCommand>
{
    public SendMessageCommandValidator()
    {
        RuleFor(x => x.SenderId)
            .GreaterThan(0).WithMessage("SenderId must be a valid user ID.");

        RuleFor(x => x.ReceiverId)
            .GreaterThan(0).WithMessage("ReceiverId must be a valid user ID.")
            .NotEqual(x => x.SenderId).WithMessage("Sender and Receiver cannot be the same user.");

        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Message text is required.")
            .MaximumLength(5000).WithMessage("Message text must not exceed 5000 characters.");
    }
}

