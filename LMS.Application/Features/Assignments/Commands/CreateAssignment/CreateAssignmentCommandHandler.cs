using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Domain.Enums;
using MediatR;

namespace LMS.Application.Features.Assignments.Commands.CreateAssignment;

public class CreateAssignmentCommandHandler : IRequestHandler<CreateAssignmentCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;

    public CreateAssignmentCommandHandler(IUnitOfWork unitOfWork, IUserRepository userRepository)
    {
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
    }

    public async Task<int> Handle(CreateAssignmentCommand request, CancellationToken cancellationToken)
    {
        // Check if CourseInstance exists
        var courseInstance = await _unitOfWork.CourseInstances.GetByIdAsync(request.CourseInstanceId);
        if (courseInstance == null)
        {
            throw new InvalidOperationException($"CourseInstance with ID {request.CourseInstanceId} not found.");
        }

        // Authorization check: Only Teacher (assigned to CourseInstance/Group) or Admin can create
        var createdByUser = await _userRepository.GetUserByIdAsync(request.CreatedById);
        if (createdByUser == null)
        {
            throw new UnauthorizedAccessException("Invalid user performing the action.");
        }

        if (createdByUser.Role == UserRole.Teacher)
        {
            // Check if teacher is assigned to the group
            var isTeacherInGroup = await _unitOfWork.GroupUsers.IsUserAssignedToGroupAsync(courseInstance.GroupId, request.CreatedById);
            if (!isTeacherInGroup)
            {
                throw new UnauthorizedAccessException("Teacher must be assigned to the CourseInstance/Group to create assignments.");
            }
        }
        else if (createdByUser.Role == UserRole.Mentor)
        {
            throw new UnauthorizedAccessException("Mentor cannot create assignments. Only Teacher (assigned to CourseInstance/Group) or Admin can create assignments.");
        }
        else if (createdByUser.Role != UserRole.MasterAdmin && createdByUser.Role != UserRole.Admin)
        {
            throw new UnauthorizedAccessException("Only Teacher (assigned to CourseInstance/Group) or Admin can create assignments.");
        }

        // Business rule: ReadingMaterial type cannot be scored (MaxScore ignored)
        var maxScore = request.AssignmentType == AssignmentType.ReadingMaterial ? 0 : request.MaxScore;

        var assignment = new Assignment
        {
            CourseInstanceId = request.CourseInstanceId,
            GroupId = courseInstance.GroupId,
            Title = request.Title,
            Description = request.Description,
            AssignmentType = request.AssignmentType,
            MaxScore = maxScore,
            Deadline = request.Deadline,
            AllowEditAfterPublish = request.AllowEditAfterPublish,
            AllowResubmit = request.AllowResubmit,
            CreatedById = request.CreatedById,
            IsPublished = false, // Initially unpublished
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Assignments.AddAsync(assignment);
        await _unitOfWork.SaveChangesAsync();

        return assignment.Id;
    }
}

