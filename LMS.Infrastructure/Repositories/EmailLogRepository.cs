using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LMS.Infrastructure.Repositories;

public class EmailLogRepository : EfRepository<EmailLog>, IEmailLogRepository
{
    public EmailLogRepository(LmsDbContext context) : base(context)
    {
    }

    public async Task<List<EmailLog>> GetEmailLogsAsync(
        string? to = null,
        string? status = null,
        string? templateType = null,
        int? userId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int? pageNumber = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsQueryable();

        if (!string.IsNullOrEmpty(to))
        {
            query = query.Where(el => el.To.Contains(to));
        }

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(el => el.Status == status);
        }

        if (!string.IsNullOrEmpty(templateType))
        {
            query = query.Where(el => el.TemplateType == templateType);
        }

        if (userId.HasValue)
        {
            query = query.Where(el => el.UserId == userId.Value);
        }

        if (startDate.HasValue)
        {
            query = query.Where(el => el.SentAt >= startDate.Value);
        }

        if (endDate.HasValue)
        {
            query = query.Where(el => el.SentAt <= endDate.Value);
        }

        query = query.OrderByDescending(el => el.SentAt);

        if (pageNumber.HasValue && pageSize.HasValue)
        {
            query = query.Skip((pageNumber.Value - 1) * pageSize.Value).Take(pageSize.Value);
        }

        return await query.ToListAsync(cancellationToken);
    }

    public async Task<List<EmailLog>> GetEmailLogsByUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(el => el.UserId == userId)
            .OrderByDescending(el => el.SentAt)
            .ToListAsync(cancellationToken);
    }
}


