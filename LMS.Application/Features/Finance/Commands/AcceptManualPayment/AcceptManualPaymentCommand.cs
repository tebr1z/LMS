using MediatR;

namespace LMS.Application.Features.Finance.Commands.AcceptManualPayment;

public class AcceptManualPaymentCommand : IRequest<int>
{
    public int PaymentId { get; set; }
    public string? Reference { get; set; } // Cash/post-terminal reference
    public string? Notes { get; set; }
    public int AcceptedById { get; set; } // Finance user accepting the payment
}

