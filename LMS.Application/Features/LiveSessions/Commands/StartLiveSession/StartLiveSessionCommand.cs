using MediatR;

namespace LMS.Application.Features.LiveSessions.Commands.StartLiveSession;

public class StartLiveSessionCommand : IRequest<StartLiveSessionResultDto>
{
    public int GroupId { get; set; }
    public int TeacherId { get; set; }
}

public class StartLiveSessionResultDto
{
    public int SessionId { get; set; }
    public string SessionUrl { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
}

