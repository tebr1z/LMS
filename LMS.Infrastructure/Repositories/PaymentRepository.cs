using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Domain.Enums;
using LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LMS.Infrastructure.Repositories;

public class PaymentRepository : EfRepository<Payment>, IPaymentRepository
{
    public PaymentRepository(LmsDbContext context) : base(context)
    {
    }

    public async Task<List<Payment>> GetPaymentsByStudentIdAsync(int studentId)
    {
        return await _dbSet
            .Where(p => p.StudentId == studentId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Payment>> GetPaymentsByStatusAsync(PaymentStatus status)
    {
        return await _dbSet
            .Where(p => p.Status == status)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Payment>> GetOverduePaymentsAsync()
    {
        var now = DateTime.UtcNow;
        return await _dbSet
            .Where(p => p.DueDate.HasValue && 
                       p.DueDate.Value < now && 
                       p.Status != PaymentStatus.Completed &&
                       p.Status != PaymentStatus.Manual &&
                       p.Status != PaymentStatus.Deferred)
            .OrderBy(p => p.DueDate)
            .ToListAsync();
    }

    public async Task<Payment?> GetPaymentByProviderTransactionIdAsync(string providerTransactionId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(p => p.ProviderTransactionId == providerTransactionId);
    }

    public async Task<List<Payment>> GetPaymentsByInvoiceIdAsync(int invoiceId)
    {
        return await _dbSet
            .Where(p => p.InvoiceId == invoiceId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }
}

