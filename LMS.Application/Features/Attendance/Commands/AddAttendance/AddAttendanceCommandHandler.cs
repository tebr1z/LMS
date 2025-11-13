using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Domain.Enums;
using MediatR;

namespace LMS.Application.Features.Attendance.Commands.AddAttendance;

public class AddAttendanceCommandHandler : IRequestHandler<AddAttendanceCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;

    public AddAttendanceCommandHandler(IUnitOfWork unitOfWork, IUserRepository userRepository)
    {
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
    }

    public async Task<int> Handle(AddAttendanceCommand request, CancellationToken cancellationToken)
    {
        // Verify group exists
        var group = await _unitOfWork.Groups.GetByIdAsync(request.GroupId);
        if (group == null)
        {
            throw new InvalidOperationException($"Group with ID {request.GroupId} not found.");
        }

        // Verify student exists and belongs to group
        var isStudentInGroup = await _unitOfWork.Groups.IsUserInGroupAsync(request.GroupId, request.StudentId);
        if (!isStudentInGroup)
        {
            throw new InvalidOperationException($"Student with ID {request.StudentId} is not a member of group {request.GroupId}.");
        }

        // Verify user marking attendance
        var marker = await _userRepository.GetUserByIdAsync(request.MarkedById);
        if (marker == null)
        {
            throw new UnauthorizedAccessException("Invalid user marking attendance.");
        }

        // Verify marker belongs to group and has appropriate role (Mentor, Teacher, or StudentOffice)
        var isMarkerInGroup = await _unitOfWork.Groups.IsUserInGroupAsync(request.GroupId, request.MarkedById);
        if (!isMarkerInGroup)
        {
            throw new UnauthorizedAccessException("User marking attendance must belong to the group.");
        }

        // Check if user has appropriate role (Mentor, Teacher, StudentOffice, or Admin)
        var groupUser = await _unitOfWork.GroupUsers.GetByGroupAndUserAsync(request.GroupId, request.MarkedById);
        bool canMarkAttendance = marker.Role == UserRole.MasterAdmin || 
                                 marker.Role == UserRole.Admin ||
                                 marker.Role == UserRole.StudentOffice ||
                                 (groupUser != null && (groupUser.Role == GroupRole.Mentor || groupUser.Role == GroupRole.Teacher));

        if (!canMarkAttendance)
        {
            throw new UnauthorizedAccessException("Only Mentor, Teacher, StudentOffice, or Admin can mark attendance.");
        }

        // Check if attendance already exists for this student on this date
        var existingAttendance = await _unitOfWork.Attendances.GetAttendanceByGroupStudentAndDateAsync(
            request.GroupId, request.StudentId, request.Date);

        if (existingAttendance != null)
        {
            // Update existing attendance
            existingAttendance.Present = request.Present;
            existingAttendance.Notes = request.Notes;
            existingAttendance.MarkedById = request.MarkedById;
            existingAttendance.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Attendances.UpdateAsync(existingAttendance);
            await _unitOfWork.SaveChangesAsync();

            return existingAttendance.Id;
        }

        // Create new attendance
        var attendance = new Attendance
        {
            GroupId = request.GroupId,
            StudentId = request.StudentId,
            Date = request.Date.Date, // Store only date part
            Present = request.Present,
            Notes = request.Notes,
            MarkedById = request.MarkedById,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Attendances.AddAsync(attendance);
        await _unitOfWork.SaveChangesAsync();

        return attendance.Id;
    }
}

