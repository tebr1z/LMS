using LMS.Application.Features.Messages.Commands.SendMessage;
using LMS.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace LMS.API.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IMediator _mediator;
    private readonly IUnitOfWork _unitOfWork;
    private static readonly Dictionary<int, HashSet<string>> _userConnections = new(); // Multiple connections per user

    public ChatHub(IMediator mediator, IUnitOfWork unitOfWork)
    {
        _mediator = mediator;
        _unitOfWork = unitOfWork;
    }

    public override async Task OnConnectedAsync()
    {
        // Authenticate via JWT in query string or header and record connection id -> map to user
        var userId = GetUserId();
        if (userId.HasValue)
        {
            // Track connection id -> map to user
            if (!_userConnections.ContainsKey(userId.Value))
            {
                _userConnections[userId.Value] = new HashSet<string>();
            }
            _userConnections[userId.Value].Add(Context.ConnectionId);
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Remove connection mapping
        var userId = GetUserId();
        if (userId.HasValue && _userConnections.ContainsKey(userId.Value))
        {
            _userConnections[userId.Value].Remove(Context.ConnectionId);
            if (_userConnections[userId.Value].Count == 0)
            {
                _userConnections.Remove(userId.Value);
            }
        }
        await base.OnDisconnectedAsync(exception);
    }

    // Private message support (optional)
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
        if (_userConnections.ContainsKey(receiverId))
        {
            foreach (var connectionId in _userConnections[receiverId])
            {
                await Clients.Client(connectionId).SendAsync("ReceiveMessage", new
                {
                    MessageId = messageId,
                    SenderId = senderId.Value,
                    ReceiverId = receiverId,
                    Text = text,
                    SentAt = DateTime.UtcNow
                });
            }
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

    /// <summary>
    /// Send a message to all users in a group
    /// </summary>
    public async Task SendGroupMessage(int groupId, string message)
    {
        var senderId = GetUserId();
        if (!senderId.HasValue)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        // Check user belongs to group
        var isUserInGroup = await _unitOfWork.Groups.IsUserInGroupAsync(groupId, senderId.Value);
        if (!isUserInGroup)
        {
            throw new UnauthorizedAccessException("User is not a member of this group.");
        }

        // Verify group exists
        var group = await _unitOfWork.Groups.GetByIdAsync(groupId);
        if (group == null)
        {
            throw new InvalidOperationException($"Group with ID {groupId} not found.");
        }

        // Save message to Message table with GroupId
        var command = new SendMessageCommand
        {
            SenderId = senderId.Value,
            ReceiverId = 0, // Group message
            GroupId = groupId,
            Text = message
        };

        var messageId = await _mediator.Send(command);

        // Prepare payload
        var payload = new
        {
            MessageId = messageId,
            GroupId = groupId,
            SenderId = senderId.Value,
            Text = message,
            SentAt = DateTime.UtcNow
        };

        // Send to group: Clients.Group(GetGroupName(groupId)).SendAsync("ReceiveGroupMessage", payload)
        var groupName = GetGroupName(groupId);
        await Clients.Group(groupName).SendAsync("ReceiveGroupMessage", payload);
    }

    /// <summary>
    /// Join a group -> add connection to SignalR group
    /// </summary>
    public async Task JoinGroup(int groupId)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        // Check user belongs to group
        var isUserInGroup = await _unitOfWork.Groups.IsUserInGroupAsync(groupId, userId.Value);
        if (!isUserInGroup)
        {
            throw new UnauthorizedAccessException("User is not a member of this group.");
        }

        // Add connection to SignalR group
        var groupName = GetGroupName(groupId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        await Clients.Caller.SendAsync("JoinedGroup", new { GroupId = groupId, GroupName = groupName });
    }

    /// <summary>
    /// Leave a group
    /// </summary>
    public async Task LeaveGroup(int groupId)
    {
        var groupName = GetGroupName(groupId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        await Clients.Caller.SendAsync("LeftGroup", new { GroupId = groupId, GroupName = groupName });
    }

    private string GetGroupName(int groupId)
    {
        return $"group_{groupId}";
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

