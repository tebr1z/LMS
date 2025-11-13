using LMS.Domain.Enums;
using MediatR;

namespace LMS.Application.Features.Finance.Queries.GetPayments;

public class GetPaymentsQuery : IRequest<IEnumerable<PaymentDto>>
{
    public int? StudentId { get; set; } // Optional: filter by student
    public PaymentStatus? Status { get; set; } // Optional: filter by status
    public PaymentMethod? PaymentMethod { get; set; } // Optional: filter by payment method
}

public class PaymentDto
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public PaymentMethod PaymentMethod { get; set; }
    public string PaymentMethodName { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime? PaidAt { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Reference { get; set; }
    public string? ProviderTransactionId { get; set; }
    public string? Notes { get; set; }
    public int? InvoiceId { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateTime CreatedAt { get; set; }
}

