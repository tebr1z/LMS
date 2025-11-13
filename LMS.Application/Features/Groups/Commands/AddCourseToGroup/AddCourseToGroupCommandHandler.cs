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

        // Check if CoursePrepared exists
        var coursePrepared = await _unitOfWork.CoursePrepareds.GetByIdAsync(request.CoursePreparedId);
        if (coursePrepared == null)
        {
            throw new InvalidOperationException($"CoursePrepared with ID {request.CoursePreparedId} not found.");
        }

        // Check if CoursePrepared is already in the group
        var existingCourseGroup = await _unitOfWork.CourseGroups.GetByGroupAndCourseAsync(request.GroupId, request.CoursePreparedId);
        if (existingCourseGroup != null)
        {
            throw new InvalidOperationException($"CoursePrepared {request.CoursePreparedId} is already in group {request.GroupId}.");
        }

        // Authorization check: Only MasterAdmin or Admin can add courses to groups
        var addedByUser = await _userRepository.GetUserByIdAsync(request.AddedBy);
        if (addedByUser == null)
        {
            throw new UnauthorizedAccessException("Invalid user performing the action.");
        }

        if (addedByUser.Role != UserRole.MasterAdmin && addedByUser.Role != UserRole.Admin)
        {
            throw new UnauthorizedAccessException("Only MasterAdmin or Admin can add courses to groups.");
        }

        // When CoursePrepared is added to Group, create a Course instance (CourseTaken) with copy of DefaultContent
        var courseTaken = new Course
        {
            Title = coursePrepared.Title,
            Description = coursePrepared.Description,
            CreatedBy = request.AddedBy,
            CreatedAt = DateTime.UtcNow
        };

        // Note: Course entity doesn't have a Content field, but if it had, we would copy:
        // courseTaken.Content = coursePrepared.DefaultContent;
        // Assignments remain separate (they stay with CoursePrepared)

        // Add CourseTaken to database
        await _unitOfWork.Courses.AddAsync(courseTaken);
        await _unitOfWork.SaveChangesAsync();

        // Create CourseGroup link between Group and CoursePrepared
        var courseGroup = new CourseGroup
        {
            GroupId = request.GroupId,
            CoursePreparedId = request.CoursePreparedId,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.CourseGroups.AddAsync(courseGroup);
        await _unitOfWork.SaveChangesAsync();

        return courseGroup.Id;
    }
}

