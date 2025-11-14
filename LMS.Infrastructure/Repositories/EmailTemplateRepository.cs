using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LMS.Infrastructure.Repositories;

public class EmailTemplateRepository : EfRepository<EmailTemplate>, IEmailTemplateRepository
{
    public EmailTemplateRepository(LmsDbContext context) : base(context)
    {
    }

    public async Task<EmailTemplate?> GetByTemplateTypeAsync(string templateType, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(et => et.TemplateType == templateType && et.IsActive, cancellationToken);
    }

    public async Task<List<EmailTemplate>> GetActiveTemplatesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(et => et.IsActive)
            .OrderBy(et => et.TemplateType)
            .ToListAsync(cancellationToken);
    }
}


