using MediatR;

namespace LMS.Application.Features.LiveSessions.Commands.EndLiveSession;

public class EndLiveSessionCommand : IRequest<Unit>
{
    public int SessionId { get; set; }
    public int TeacherId { get; set; }
}

