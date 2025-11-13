using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Domain.Enums;
using MediatR;

namespace LMS.Application.Features.Groups.Commands.AddUserToGroup;

public class AddUserToGroupCommandHandler : IRequestHandler<AddUserToGroupCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;

    public AddUserToGroupCommandHandler(IUnitOfWork unitOfWork, IUserRepository userRepository)
    {
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
    }

    public async Task<int> Handle(AddUserToGroupCommand request, CancellationToken cancellationToken)
    {
        // Check if group exists
        var group = await _unitOfWork.Groups.GetByIdAsync(request.GroupId);
        if (group == null)
        {
            throw new InvalidOperationException($"Group with ID {request.GroupId} not found.");
        }

        // Check if user exists
        var user = await _userRepository.GetUserByIdAsync(request.UserId);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {request.UserId} not found.");
        }

        // Check if user is already in the group
        var existingGroupUser = await _unitOfWork.GroupUsers.GetByGroupAndUserAsync(request.GroupId, request.UserId);
        if (existingGroupUser != null)
        {
            throw new InvalidOperationException($"User {request.UserId} is already in group {request.GroupId}.");
        }

        // Authorization check: Only MasterAdmin, Admin, or Teacher (if assigned to group) can add users
        var addedByUser = await _userRepository.GetUserByIdAsync(request.AddedBy);
        if (addedByUser == null)
        {
            throw new UnauthorizedAccessException("Invalid user performing the action.");
        }

        // Business rule: Teacher can add Student/Mentor only to groups they belong to
        if (addedByUser.Role == UserRole.Teacher)
        {
            var isTeacherInGroup = await _unitOfWork.GroupUsers.IsUserAssignedToGroupAsync(request.GroupId, request.AddedBy);
            if (!isTeacherInGroup)
            {
                throw new UnauthorizedAccessException("Teacher must be assigned to the group to add users.");
            }

            // Teacher can only add Student or Mentor roles
            if (request.Role != GroupRole.Student && request.Role != GroupRole.Mentor)
            {
                throw new UnauthorizedAccessException("Teachers can only add Students or Mentors to groups.");
            }
        }
        else if (addedByUser.Role != UserRole.MasterAdmin && addedByUser.Role != UserRole.Admin)
        {
            throw new UnauthorizedAccessException("Only MasterAdmin, Admin, or assigned Teacher can add users to groups.");
        }

        var groupUser = new GroupUser
        {
            GroupId = request.GroupId,
            UserId = request.UserId,
            Role = request.Role,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.GroupUsers.AddAsync(groupUser);
        await _unitOfWork.SaveChangesAsync();

        return groupUser.Id;
    }
}

