using LMS.Application.Interfaces;
using MediatR;

namespace LMS.Application.Features.Notifications.Queries.GetNotifications;

public class GetNotificationsQueryHandler : IRequestHandler<GetNotificationsQuery, IEnumerable<NotificationDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetNotificationsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<IEnumerable<NotificationDto>> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        List<Domain.Entities.Notification> notifications;

        if (request.Count.HasValue)
        {
            notifications = await _unitOfWork.Notifications.GetRecentByUserIdAsync(request.UserId, request.Count.Value);
        }
        else if (request.IsRead.HasValue)
        {
            notifications = await _unitOfWork.Notifications.GetByUserIdAsync(request.UserId, request.IsRead.Value);
        }
        else
        {
            notifications = await _unitOfWork.Notifications.GetByUserIdAsync(request.UserId);
        }

        return notifications.Select(n => new NotificationDto
        {
            Id = n.Id,
            Title = n.Title,
            Body = n.Body,
            Data = n.Data,
            IsRead = n.IsRead,
            CreatedAt = n.CreatedAt,
            ReadAt = n.ReadAt,
            Channel = n.Channel.ToString(),
            Type = n.Type
        });
    }
}

