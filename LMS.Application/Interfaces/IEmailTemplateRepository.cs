using LMS.Domain.Entities;

namespace LMS.Application.Interfaces;

public interface IEmailTemplateRepository : IRepository<EmailTemplate>
{
    Task<EmailTemplate?> GetByTemplateTypeAsync(string templateType, CancellationToken cancellationToken = default);
    Task<List<EmailTemplate>> GetActiveTemplatesAsync(CancellationToken cancellationToken = default);
}


