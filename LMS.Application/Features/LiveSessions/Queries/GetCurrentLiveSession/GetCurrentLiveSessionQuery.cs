using MediatR;

namespace LMS.Application.Features.LiveSessions.Queries.GetCurrentLiveSession;

public class GetCurrentLiveSessionQuery : IRequest<CurrentLiveSessionDto?>
{
    public int GroupId { get; set; }
    public int UserId { get; set; }
    public string UserRole { get; set; } = string.Empty;
}

public class CurrentLiveSessionDto
{
    public int SessionId { get; set; }
    public string SessionUrl { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public int TeacherId { get; set; }
    public string TeacherName { get; set; } = string.Empty;
}

