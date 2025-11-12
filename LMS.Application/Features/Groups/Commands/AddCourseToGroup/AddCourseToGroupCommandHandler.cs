using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Domain.Enums;
using MediatR;

namespace LMS.Application.Features.Groups.Commands.AddCourseToGroup;

public class AddCourseToGroupCommandHandler : IRequestHandler<AddCourseToGroupCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;

    public AddCourseToGroupCommandHandler(IUnitOfWork unitOfWork, IUserRepository userRepository)
    {
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
    }

    public async Task<int> Handle(AddCourseToGroupCommand request, CancellationToken cancellationToken)
    {
        // Check if group exists
        var group = await _unitOfWork.Groups.GetByIdAsync(request.GroupId);
        if (group == null)
        {
            throw new InvalidOperationException($"Group with ID {request.GroupId} not found.");
        }

        // Check if course exists
        var course = await _unitOfWork.Courses.GetByIdAsync(request.CourseId);
        if (course == null)
        {
            throw new InvalidOperationException($"Course with ID {request.CourseId} not found.");
        }

        // Check if course is already in the group
        var existingCourseGroup = await _unitOfWork.CourseGroups.GetByGroupAndCourseAsync(request.GroupId, request.CourseId);
        if (existingCourseGroup != null)
        {
            throw new InvalidOperationException($"Course {request.CourseId} is already in group {request.GroupId}.");
        }

        // Authorization check: Only MasterAdmin, Admin, or Teacher (if assigned to group) can add courses
        var addedByUser = await _userRepository.GetUserByIdAsync(request.AddedBy);
        if (addedByUser == null)
        {
            throw new UnauthorizedAccessException("Invalid user performing the action.");
        }

        // Check if user is Teacher and if they are assigned to this group
        if (addedByUser.Role == UserRole.Teacher)
        {
            var isTeacherInGroup = await _unitOfWork.GroupUsers.IsUserAssignedToGroupAsync(request.GroupId, request.AddedBy);
            if (!isTeacherInGroup)
            {
                throw new UnauthorizedAccessException("Teacher must be assigned to the group to add courses.");
            }
        }
        else if (addedByUser.Role != UserRole.MasterAdmin && addedByUser.Role != UserRole.Admin)
        {
            throw new UnauthorizedAccessException("Only MasterAdmin, Admin, or assigned Teacher can add courses to groups.");
        }

        var courseGroup = new CourseGroup
        {
            GroupId = request.GroupId,
            CourseId = request.CourseId,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.CourseGroups.AddAsync(courseGroup);
        await _unitOfWork.SaveChangesAsync();

        return courseGroup.Id;
    }
}

