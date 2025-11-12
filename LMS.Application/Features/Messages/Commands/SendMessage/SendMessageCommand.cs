using MediatR;

namespace LMS.Application.Features.Messages.Commands.SendMessage;

public class SendMessageCommand : IRequest<int>
{
    public int SenderId { get; set; }
    public int ReceiverId { get; set; }
    public int? GroupId { get; set; } // Optional: if set, message is a group chat message
    public string Text { get; set; } = string.Empty;
}

