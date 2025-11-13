using MediatR;

namespace LMS.Application.Features.StudentPayments.Commands.InitiatePayment;

public class InitiatePaymentCommand : IRequest<PaymentSessionDto>
{
    public int PaymentId { get; set; }
    public int StudentId { get; set; } // To verify ownership
}

public class PaymentSessionDto
{
    public string PaymentSessionId { get; set; } = string.Empty;
    public string RedirectUrl { get; set; } = string.Empty; // URL to redirect student to bank payment page
    public string? ProviderSessionId { get; set; } // Provider-specific session ID (OneBank, etc.)
    public DateTime ExpiresAt { get; set; }
}

