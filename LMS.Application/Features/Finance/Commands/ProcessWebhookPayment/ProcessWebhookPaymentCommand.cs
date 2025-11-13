using MediatR;

namespace LMS.Application.Features.Finance.Commands.ProcessWebhookPayment;

public class ProcessWebhookPaymentCommand : IRequest<bool>
{
    public string ProviderTransactionId { get; set; } = string.Empty;
    public string? Reference { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "AZN";
    public DateTime? PaidAt { get; set; }
    public Dictionary<string, object>? ProviderData { get; set; } // Additional provider-specific data
}

