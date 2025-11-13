using LMS.Application.Interfaces;
using MediatR;

namespace LMS.Application.Features.LiveSessions.Commands.StartLiveSession;

public class StartLiveSessionCommandHandler : IRequestHandler<StartLiveSessionCommand, StartLiveSessionResultDto>
{
    private readonly ILiveSessionService _liveSessionService;
    private readonly IUnitOfWork _unitOfWork;

    public StartLiveSessionCommandHandler(
        ILiveSessionService liveSessionService,
        IUnitOfWork unitOfWork)
    {
        _liveSessionService = liveSessionService;
        _unitOfWork = unitOfWork;
    }

    public async Task<StartLiveSessionResultDto> Handle(StartLiveSessionCommand request, CancellationToken cancellationToken)
    {
        var sessionUrl = await _liveSessionService.StartSessionAsync(request.GroupId, request.TeacherId, cancellationToken);

        // Get the created session to return its ID
        var session = await _unitOfWork.LiveSessions.GetActiveSessionByGroupIdAsync(request.GroupId, cancellationToken);
        
        if (session == null)
        {
            throw new InvalidOperationException("Failed to retrieve created session.");
        }

        return new StartLiveSessionResultDto
        {
            SessionId = session.Id,
            SessionUrl = sessionUrl,
            StartTime = session.StartTime
        };
    }
}

