using MediatR;

namespace LMS.Application.Features.Notifications.Queries.GetNotifications;

public class GetNotificationsQuery : IRequest<IEnumerable<NotificationDto>>
{
    public int UserId { get; set; }
    public bool? IsRead { get; set; } // Optional: filter by read status
    public int? Count { get; set; } // Optional: limit number of results
}

public class NotificationDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? Data { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReadAt { get; set; }
    public string Channel { get; set; } = string.Empty;
    public string? Type { get; set; }
}

