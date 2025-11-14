using LMS.Domain.Entities;

namespace LMS.Application.Interfaces;

public interface IEmailLogRepository : IRepository<EmailLog>
{
    Task<List<EmailLog>> GetEmailLogsAsync(
        string? to = null,
        string? status = null,
        string? templateType = null,
        int? userId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        int? pageNumber = null,
        int? pageSize = null,
        CancellationToken cancellationToken = default);
    
    Task<List<EmailLog>> GetEmailLogsByUserAsync(int userId, CancellationToken cancellationToken = default);
}


