using LMS.Domain.Enums;
using MediatR;

namespace LMS.Application.Features.StudentPayments.Queries.GetStudentPayments;

public class GetStudentPaymentsQuery : IRequest<StudentPaymentsDto>
{
    public int StudentId { get; set; }
}

public class StudentPaymentsDto
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public List<InvoiceDto> Invoices { get; set; } = new();
    public List<PaymentSummaryDto> Payments { get; set; } = new();
    public PaymentSummary Summary { get; set; } = new();
}

public class InvoiceDto
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public bool PaidStatus { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PaymentSummaryDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = string.Empty;
    public PaymentMethod PaymentMethod { get; set; }
    public string PaymentMethodName { get; set; } = string.Empty;
    public PaymentStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public DateTime? DueDate { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? Reference { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PaymentSummary
{
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal PendingAmount { get; set; }
    public decimal OverdueAmount { get; set; }
    public int TotalPayments { get; set; }
    public int PaidPayments { get; set; }
    public int PendingPayments { get; set; }
    public int OverduePayments { get; set; }
}

