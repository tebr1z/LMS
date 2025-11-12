using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Domain.Enums;
using LMS.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace LMS.Application.Features.Groups.Commands.AddUserToGroup;

public class AddUserToGroupCommandHandler : IRequestHandler<AddUserToGroupCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<ApplicationUser> _userManager;

    public AddUserToGroupCommandHandler(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
    {
        _unitOfWork = unitOfWork;
        _userManager = userManager;
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
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
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
        var addedByUser = await _userManager.FindByIdAsync(request.AddedBy.ToString());
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
                throw new UnauthorizedAccessException("Teacher must be assigned to the group to add users.");
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

