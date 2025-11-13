using LMS.Domain.Entities;

namespace LMS.Application.Interfaces;

public interface IInvoiceRepository : IRepository<Invoice>
{
    Task<List<Invoice>> GetInvoicesByStudentIdAsync(int studentId);
    Task<List<Invoice>> GetUnpaidInvoicesAsync(); // Invoices with PaidStatus = false
    Task<Invoice?> GetInvoiceByInvoiceNumberAsync(string invoiceNumber);
    Task<List<Invoice>> GetOverdueInvoicesAsync(); // Invoices past DueDate with PaidStatus = false
}

