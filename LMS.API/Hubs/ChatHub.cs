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
    private static readonly Dictionary<int, string> _userConnections = new();

    public ChatHub(IMediator mediator, IUnitOfWork unitOfWork)
    {
        _mediator = mediator;
        _unitOfWork = unitOfWork;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = GetUserId();
        if (userId.HasValue)
        {
            _userConnections[userId.Value] = Context.ConnectionId;
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId.Value}");
            
            // Add user to SignalR groups for all groups they belong to
            await JoinUserGroupsAsync(userId.Value);
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

    /// <summary>
    /// Send a message to all users in a group
    /// </summary>
    public async Task SendGroupMessage(int groupId, string text)
    {
        var senderId = GetUserId();
        if (!senderId.HasValue)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        // Verify user is a member of the group
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

        // Get all users in the group
        var groupUsers = await _unitOfWork.GroupUsers.GetGroupUsersByGroupIdAsync(groupId);
        if (groupUsers.Count == 0)
        {
            throw new InvalidOperationException("Group has no members.");
        }

        // Save message to database for each group member (except sender)
        // For group messages, ReceiverId can be 0 or we can save one message per member
        // We'll save one message per member for easier querying
        var messageIds = new List<int>();
        foreach (var groupUser in groupUsers)
        {
            if (groupUser.UserId != senderId.Value) // Don't save message for sender
            {
                var command = new SendMessageCommand
                {
                    SenderId = senderId.Value,
                    ReceiverId = groupUser.UserId,
                    GroupId = groupId,
                    Text = text
                };

                var messageId = await _mediator.Send(command);
                messageIds.Add(messageId);
            }
        }

        // Also save a message with ReceiverId = 0 for group history (optional)
        // For now, we'll use the first message ID as the group message ID

        // Broadcast to all users in the group via SignalR
        await Clients.Group($"group_{groupId}").SendAsync("ReceiveGroupMessage", new
        {
            MessageId = messageIds.FirstOrDefault(),
            GroupId = groupId,
            SenderId = senderId.Value,
            Text = text,
            SentAt = DateTime.UtcNow
        });

        // Send confirmation to sender
        await Clients.Caller.SendAsync("GroupMessageSent", new
        {
            MessageId = messageIds.FirstOrDefault(),
            GroupId = groupId,
            Text = text,
            SentAt = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Join a specific group chat
    /// </summary>
    public async Task JoinGroup(int groupId)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        // Verify user is a member of the group
        var isUserInGroup = await _unitOfWork.Groups.IsUserInGroupAsync(groupId, userId.Value);
        if (!isUserInGroup)
        {
            throw new UnauthorizedAccessException("User is not a member of this group.");
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"group_{groupId}");
        await Clients.Caller.SendAsync("JoinedGroup", new { GroupId = groupId });
    }

    /// <summary>
    /// Leave a specific group chat
    /// </summary>
    public async Task LeaveGroup(int groupId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"group_{groupId}");
        await Clients.Caller.SendAsync("LeftGroup", new { GroupId = groupId });
    }

    /// <summary>
    /// Join all groups that the user belongs to
    /// </summary>
    private async Task JoinUserGroupsAsync(int userId)
    {
        // Get all groups the user belongs to
        var allGroupUsers = await _unitOfWork.GroupUsers.ListAsync();
        var userGroups = allGroupUsers.Where(gu => gu.UserId == userId).ToList();

        foreach (var groupUser in userGroups)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"group_{groupUser.GroupId}");
        }
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

