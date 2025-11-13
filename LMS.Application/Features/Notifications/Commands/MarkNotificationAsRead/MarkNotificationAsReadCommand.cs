using MediatR;

namespace LMS.Application.Features.Notifications.Commands.MarkNotificationAsRead;

public class MarkNotificationAsReadCommand : IRequest<bool>
{
    public int NotificationId { get; set; }
    public int UserId { get; set; } // To ensure user can only mark their own notifications
}

