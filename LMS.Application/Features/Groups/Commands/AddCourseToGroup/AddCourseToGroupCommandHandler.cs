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

        // Check if CourseInstance already exists for this CoursePrepared and Group (unique constraint)
        var existingCourseInstance = await _unitOfWork.CourseInstances.GetByCoursePreparedAndGroupAsync(request.CoursePreparedId, request.GroupId);
        if (existingCourseInstance != null)
        {
            throw new InvalidOperationException($"CourseInstance for CoursePrepared {request.CoursePreparedId} already exists in group {request.GroupId}.");
        }

        // Check if CoursePrepared is already linked to the group
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

        // 1) Create CourseInstance copying CoursePrepared.DefaultContent -> CourseInstance.Content
        var courseInstance = new CourseInstance
        {
            CoursePreparedId = request.CoursePreparedId,
            GroupId = request.GroupId,
            Title = coursePrepared.Title,
            Description = coursePrepared.Description,
            Content = coursePrepared.DefaultContent, // Copy DefaultContent to Content
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.CourseInstances.AddAsync(courseInstance);
        await _unitOfWork.SaveChangesAsync();

        // 2) Copy template assignments metadata if "copyAssignmentsFlag" true, else leave empty
        if (request.CopyAssignmentsFlag)
        {
            var templateAssignments = await _unitOfWork.Assignments.GetAssignmentsByCoursePreparedIdAsync(request.CoursePreparedId);
            
            foreach (var templateAssignment in templateAssignments)
            {
                var instanceAssignment = new Assignment
                {
                    CourseInstanceId = courseInstance.Id,
                    GroupId = request.GroupId,
                    Title = templateAssignment.Title,
                    Description = templateAssignment.Description,
                    AssignmentType = templateAssignment.AssignmentType,
                    MaxScore = templateAssignment.MaxScore,
                    CreatedById = request.AddedBy,
                    AllowEditAfterPublish = templateAssignment.AllowEditAfterPublish,
                    AllowResubmit = templateAssignment.AllowResubmit,
                    Deadline = null, // Clear deadline - teacher will set schedule
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Assignments.AddAsync(instanceAssignment);
            }
        }
        // else: leave empty - teacher will add assignments manually

        // Create CourseGroup link between Group and CoursePrepared
        var courseGroup = new CourseGroup
        {
            GroupId = request.GroupId,
            CoursePreparedId = request.CoursePreparedId,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.CourseGroups.AddAsync(courseGroup);
        await _unitOfWork.SaveChangesAsync();

        // 3) Return CourseInstanceId
        return courseInstance.Id;
    }
}

