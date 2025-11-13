using LMS.Application.Interfaces;
using MediatR;

namespace LMS.Application.Features.Admin.Queries.GetAuditLogs;

public class GetAuditLogsQueryHandler : IRequestHandler<GetAuditLogsQuery, AuditLogsDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;

    public GetAuditLogsQueryHandler(IUnitOfWork unitOfWork, IUserRepository userRepository)
    {
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
    }

    public async Task<AuditLogsDto> Handle(GetAuditLogsQuery request, CancellationToken cancellationToken)
    {
        // Get audit logs with filtering
        var logs = await _unitOfWork.AuditLogs.GetAuditLogsAsync(
            entity: request.Entity,
            entityId: request.EntityId,
            action: request.Action,
            userId: request.UserId,
            startDate: request.StartDate,
            endDate: request.EndDate,
            pageNumber: request.PageNumber,
            pageSize: request.PageSize,
            cancellationToken);

        // Get total count (without pagination)
        var allLogs = await _unitOfWork.AuditLogs.GetAuditLogsAsync(
            entity: request.Entity,
            entityId: request.EntityId,
            action: request.Action,
            userId: request.UserId,
            startDate: request.StartDate,
            endDate: request.EndDate,
            pageNumber: null,
            pageSize: null,
            cancellationToken);

        var totalCount = allLogs.Count;

        // Map to DTOs
        var logDtos = new List<AuditLogDto>();
        foreach (var log in logs)
        {
            var userName = await _userRepository.GetUserFullNameAsync(log.UserId) ?? "Unknown";
            
            logDtos.Add(new AuditLogDto
            {
                Id = log.Id,
                Entity = log.Entity,
                EntityId = log.EntityId,
                Action = log.Action,
                UserId = log.UserId,
                UserName = userName,
                OldValue = log.OldValue,
                NewValue = log.NewValue,
                Timestamp = log.Timestamp,
                Description = log.Description
            });
        }

        return new AuditLogsDto
        {
            Logs = logDtos,
            TotalCount = totalCount,
            PageNumber = request.PageNumber ?? 1,
            PageSize = request.PageSize ?? 50
        };
    }
}


