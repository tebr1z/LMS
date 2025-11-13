using MediatR;

namespace LMS.Application.Features.Notifications.Commands.MarkAllNotificationsAsRead;

public class MarkAllNotificationsAsReadCommand : IRequest<bool>
{
    public int UserId { get; set; }
}

