using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LMS.Infrastructure.Repositories;

public class InvoiceRepository : EfRepository<Invoice>, IInvoiceRepository
{
    public InvoiceRepository(LmsDbContext context) : base(context)
    {
    }

    public async Task<List<Invoice>> GetInvoicesByStudentIdAsync(int studentId)
    {
        return await _dbSet
            .Where(i => i.StudentId == studentId)
            .Include(i => i.Payments)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<Invoice>> GetUnpaidInvoicesAsync()
    {
        return await _dbSet
            .Where(i => !i.PaidStatus)
            .Include(i => i.Payments)
            .OrderBy(i => i.DueDate)
            .ToListAsync();
    }

    public async Task<Invoice?> GetInvoiceByInvoiceNumberAsync(string invoiceNumber)
    {
        return await _dbSet
            .Include(i => i.Payments)
            .FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber);
    }

    public async Task<List<Invoice>> GetOverdueInvoicesAsync()
    {
        var now = DateTime.UtcNow;
        return await _dbSet
            .Where(i => !i.PaidStatus && i.DueDate < now)
            .Include(i => i.Payments)
            .OrderBy(i => i.DueDate)
            .ToListAsync();
    }
}

