using LMS.Application.Interfaces.Notifications;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace LMS.Infrastructure.Services.Notifications;

/// <summary>
/// Real-time notification service using SignalR
/// </summary>
public class SignalRNotificationService : IRealTimeNotificationService
{
    private readonly IHubContext<NotificationHub> _hubContext;
    private readonly ILogger<SignalRNotificationService> _logger;

    public SignalRNotificationService(
        IHubContext<NotificationHub> hubContext,
        ILogger<SignalRNotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task SendToUserAsync(int userId, NotificationMessage notification, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients.User(userId.ToString())
                .SendAsync("ReceiveNotification", notification, cancellationToken);
            _logger.LogInformation("Notification sent to user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification to user {UserId}", userId);
        }
    }

    public async Task SendToUsersAsync(IEnumerable<int> userIds, NotificationMessage notification, CancellationToken cancellationToken = default)
    {
        var tasks = userIds.Select(userId => SendToUserAsync(userId, notification, cancellationToken));
        await Task.WhenAll(tasks);
    }

    public async Task SendToAllAsync(NotificationMessage notification, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", notification, cancellationToken);
            _logger.LogInformation("Notification sent to all users");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification to all users");
        }
    }

    public async Task SendToGroupAsync(string groupName, NotificationMessage notification, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients.Group(groupName)
                .SendAsync("ReceiveNotification", notification, cancellationToken);
            _logger.LogInformation("Notification sent to group {GroupName}", groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending notification to group {GroupName}", groupName);
        }
    }
}

/// <summary>
/// SignalR Hub for notifications
/// </summary>
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier;
        if (!string.IsNullOrEmpty(userId))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
        }
        await base.OnDisconnectedAsync(exception);
    }
}

