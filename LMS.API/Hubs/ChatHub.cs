using LMS.Application.Features.Messages.Commands.SendMessage;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace LMS.API.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IMediator _mediator;
    private static readonly Dictionary<int, string> _userConnections = new();

    public ChatHub(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId.HasValue)
        {
            _userConnections[userId.Value] = Context.ConnectionId;
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId.Value}");
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = GetUserId();
        if (userId.HasValue)
        {
            _userConnections.Remove(userId.Value);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId.Value}");
        }
        await base.OnDisconnectedAsync(exception);
    }

    public async Task SendMessage(int receiverId, string text)
    {
        var senderId = GetUserId();
        if (!senderId.HasValue)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        // Save message to database using MediatR
        var command = new SendMessageCommand
        {
            SenderId = senderId.Value,
            ReceiverId = receiverId,
            Text = text
        };

        var messageId = await _mediator.Send(command);

        // Broadcast to receiver via SignalR
        var receiverConnectionId = _userConnections.GetValueOrDefault(receiverId);
        if (!string.IsNullOrEmpty(receiverConnectionId))
        {
            await Clients.Client(receiverConnectionId).SendAsync("ReceiveMessage", new
            {
                MessageId = messageId,
                SenderId = senderId.Value,
                ReceiverId = receiverId,
                Text = text,
                SentAt = DateTime.UtcNow
            });
        }

        // Also send confirmation to sender
        await Clients.Caller.SendAsync("MessageSent", new
        {
            MessageId = messageId,
            ReceiverId = receiverId,
            Text = text,
            SentAt = DateTime.UtcNow
        });
    }

    private int? GetUserId()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return null;
    }
}

