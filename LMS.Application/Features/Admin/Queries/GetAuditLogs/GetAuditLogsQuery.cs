using MediatR;

namespace LMS.Application.Features.Admin.Queries.GetAuditLogs;

public class GetAuditLogsQuery : IRequest<AuditLogsDto>
{
    public string? Entity { get; set; }
    public int? EntityId { get; set; }
    public string? Action { get; set; }
    public int? UserId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? PageNumber { get; set; } = 1;
    public int? PageSize { get; set; } = 50;
}

public class AuditLogsDto
{
    public List<AuditLogDto> Logs { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public class AuditLogDto
{
    public int Id { get; set; }
    public string Entity { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime Timestamp { get; set; }
    public string? Description { get; set; }
}


