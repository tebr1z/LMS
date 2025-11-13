namespace LMS.Domain.Entities;

public class Invoice : BaseEntity
{
    public int StudentId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "AZN"; // Default currency
    public DateTime DueDate { get; set; }
    public bool PaidStatus { get; set; } = false; // Whether invoice has been paid
    public DateTime? PaidAt { get; set; } // When invoice was paid
    public string InvoiceNumber { get; set; } = string.Empty; // Unique invoice number
    public string? Description { get; set; } // Invoice description

    // Navigation properties
    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}

