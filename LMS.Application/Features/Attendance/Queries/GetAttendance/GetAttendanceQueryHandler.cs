using LMS.Application.Interfaces;
using MediatR;

namespace LMS.Application.Features.Attendance.Queries.GetAttendance;

public class GetAttendanceQueryHandler : IRequestHandler<GetAttendanceQuery, IEnumerable<AttendanceDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;

    public GetAttendanceQueryHandler(IUnitOfWork unitOfWork, IUserRepository userRepository)
    {
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
    }

    public async Task<IEnumerable<AttendanceDto>> Handle(GetAttendanceQuery request, CancellationToken cancellationToken)
    {
        // Verify group exists
        var group = await _unitOfWork.Groups.GetByIdAsync(request.GroupId);
        if (group == null)
        {
            throw new InvalidOperationException($"Group with ID {request.GroupId} not found.");
        }

        List<Domain.Entities.Attendance> attendances;

        if (request.Date.HasValue && request.StudentId.HasValue)
        {
            // Get specific attendance
            var attendance = await _unitOfWork.Attendances.GetAttendanceByGroupStudentAndDateAsync(
                request.GroupId, request.StudentId.Value, request.Date.Value);
            attendances = attendance != null ? new List<Domain.Entities.Attendance> { attendance } : new List<Domain.Entities.Attendance>();
        }
        else if (request.Date.HasValue)
        {
            // Get attendance for specific date
            attendances = await _unitOfWork.Attendances.GetAttendancesByGroupAndDateAsync(request.GroupId, request.Date.Value);
        }
        else if (request.StudentId.HasValue)
        {
            // Get attendance for specific student
            attendances = await _unitOfWork.Attendances.GetAttendancesByStudentIdAsync(request.StudentId.Value);
            attendances = attendances.Where(a => a.GroupId == request.GroupId).ToList();
        }
        else
        {
            // Get all attendance for group
            attendances = await _unitOfWork.Attendances.GetAttendancesByGroupIdAsync(request.GroupId);
        }

        var attendanceDtos = new List<AttendanceDto>();

        foreach (var attendance in attendances)
        {
            attendanceDtos.Add(new AttendanceDto
            {
                Id = attendance.Id,
                GroupId = attendance.GroupId,
                GroupName = group.Name,
                StudentId = attendance.StudentId,
                StudentName = await _userRepository.GetUserFullNameAsync(attendance.StudentId) ?? "Unknown",
                Date = attendance.Date,
                Present = attendance.Present,
                Notes = attendance.Notes,
                MarkedById = attendance.MarkedById,
                MarkedByName = await _userRepository.GetUserFullNameAsync(attendance.MarkedById) ?? "Unknown",
                CreatedAt = attendance.CreatedAt
            });
        }

        return attendanceDtos;
    }
}

