using LMS.Application.Interfaces;
using MediatR;

namespace LMS.Application.Features.LiveSessions.Queries.GetCurrentLiveSession;

public class GetCurrentLiveSessionQueryHandler : IRequestHandler<GetCurrentLiveSessionQuery, CurrentLiveSessionDto?>
{
    private readonly ILiveSessionService _liveSessionService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;

    public GetCurrentLiveSessionQueryHandler(
        ILiveSessionService liveSessionService,
        IUnitOfWork unitOfWork,
        IUserRepository userRepository)
    {
        _liveSessionService = liveSessionService;
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
    }

    public async Task<CurrentLiveSessionDto?> Handle(GetCurrentLiveSessionQuery request, CancellationToken cancellationToken)
    {
        var sessionUrl = await _liveSessionService.GetCurrentSessionUrlAsync(
            request.GroupId, 
            request.UserId, 
            request.UserRole, 
            cancellationToken);

        if (sessionUrl == null)
        {
            return null;
        }

        var session = await _unitOfWork.LiveSessions.GetActiveSessionByGroupIdAsync(request.GroupId, cancellationToken);
        if (session == null)
        {
            return null;
        }

        var teacher = await _userRepository.GetUserByIdAsync(session.TeacherId);
        var teacherName = teacher?.FullName ?? "Unknown";

        return new CurrentLiveSessionDto
        {
            SessionId = session.Id,
            SessionUrl = sessionUrl,
            StartTime = session.StartTime,
            TeacherId = session.TeacherId,
            TeacherName = teacherName
        };
    }
}

