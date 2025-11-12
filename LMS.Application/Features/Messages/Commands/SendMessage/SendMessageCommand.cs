using MediatR;

namespace LMS.Application.Features.Messages.Commands.SendMessage;

public class SendMessageCommand : IRequest<int>
{
    public int SenderId { get; set; }
    public int ReceiverId { get; set; }
    public string Text { get; set; } = string.Empty;
}

