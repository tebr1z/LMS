using LMS.Application.Interfaces;
using MediatR;

namespace LMS.Application.Features.LiveSessions.Commands.EndLiveSession;

public class EndLiveSessionCommandHandler : IRequestHandler<EndLiveSessionCommand, Unit>
{
    private readonly ILiveSessionService _liveSessionService;

    public EndLiveSessionCommandHandler(ILiveSessionService liveSessionService)
    {
        _liveSessionService = liveSessionService;
    }

    public async Task<Unit> Handle(EndLiveSessionCommand request, CancellationToken cancellationToken)
    {
        await _liveSessionService.EndSessionAsync(request.SessionId, request.TeacherId, cancellationToken);
        return Unit.Value;
    }
}

