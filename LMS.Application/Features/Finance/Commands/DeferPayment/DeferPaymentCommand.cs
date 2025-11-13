using MediatR;

namespace LMS.Application.Features.Finance.Commands.DeferPayment;

public class DeferPaymentCommand : IRequest<bool>
{
    public int PaymentId { get; set; }
    public string? Notes { get; set; } // Reason for deferral
    public int DeferredById { get; set; } // Finance user deferring the payment
}

