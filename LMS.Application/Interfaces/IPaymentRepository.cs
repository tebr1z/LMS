using LMS.Domain.Entities;
using LMS.Domain.Enums;

namespace LMS.Application.Interfaces;

public interface IPaymentRepository : IRepository<Payment>
{
    Task<List<Payment>> GetPaymentsByStudentIdAsync(int studentId);
    Task<List<Payment>> GetPaymentsByStatusAsync(PaymentStatus status);
    Task<List<Payment>> GetOverduePaymentsAsync(); // Payments past DueDate with Status != Completed
    Task<Payment?> GetPaymentByProviderTransactionIdAsync(string providerTransactionId);
    Task<List<Payment>> GetPaymentsByInvoiceIdAsync(int invoiceId);
}

