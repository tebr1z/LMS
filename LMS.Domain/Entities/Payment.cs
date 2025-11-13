using LMS.Domain.Enums;

namespace LMS.Domain.Entities;

public class Payment : BaseEntity
{
    public int StudentId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "AZN"; // Default currency
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.OneTime;
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public DateTime? PaidAt { get; set; } // When payment was completed
    public DateTime? DueDate { get; set; } // Nullable due date for monthly payments
    public string? Reference { get; set; } // Payment reference (transaction ID, invoice number, etc.)
    public int? InvoiceId { get; set; } // Optional link to Invoice
    public string? ProviderTransactionId { get; set; } // Transaction ID from payment provider (OneBank, etc.)
    public string? Notes { get; set; } // Additional notes about the payment

    // Navigation properties
    public virtual Invoice? Invoice { get; set; }
}

