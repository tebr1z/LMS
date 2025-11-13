using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Domain.Enums;
using MediatR;

namespace LMS.Application.Features.Assignments.Commands.EditAssignment;

public class EditAssignmentCommandHandler : IRequestHandler<EditAssignmentCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;
    private readonly IAuditService _auditService;

    public EditAssignmentCommandHandler(
        IUnitOfWork unitOfWork,
        IUserRepository userRepository,
        IAuditService auditService)
    {
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
        _auditService = auditService;
    }

    public async Task<int> Handle(EditAssignmentCommand request, CancellationToken cancellationToken)
    {
        // Get assignment with submissions to check if published/has submissions
        var assignment = await _unitOfWork.Assignments.GetAssignmentWithSubmissionsAsync(request.AssignmentId);
        if (assignment == null)
        {
            throw new InvalidOperationException($"Assignment with ID {request.AssignmentId} not found.");
        }

        // Store old values for audit
        var oldValues = new
        {
            Title = assignment.Title,
            Description = assignment.Description,
            MaxScore = assignment.MaxScore,
            Deadline = assignment.Deadline,
            AllowEditAfterPublish = assignment.AllowEditAfterPublish,
            AllowResubmit = assignment.AllowResubmit
        };

        // Authorization check: Only creator Teacher or Admin can edit
        var editedByUser = await _userRepository.GetUserByIdAsync(request.EditedById);
        if (editedByUser == null)
        {
            throw new UnauthorizedAccessException("Invalid user performing the action.");
        }

        if (editedByUser.Role == UserRole.Mentor)
        {
            throw new UnauthorizedAccessException("Mentor cannot edit assignments. Only creator Teacher or Admin can edit assignments.");
        }
        else if (editedByUser.Role == UserRole.Teacher && assignment.CreatedById != request.EditedById)
        {
            throw new UnauthorizedAccessException("Only the creator Teacher or Admin can edit assignments.");
        }
        else if (editedByUser.Role != UserRole.MasterAdmin && editedByUser.Role != UserRole.Admin && editedByUser.Role != UserRole.Teacher)
        {
            throw new UnauthorizedAccessException("Only creator Teacher or Admin can edit assignments.");
        }

        // Business rule: If AllowEditAfterPublish == false and assignment is published/has submissions -> forbid edits
        if (!assignment.AllowEditAfterPublish && (assignment.IsPublished || assignment.Submissions.Any()))
        {
            throw new InvalidOperationException("Assignment cannot be edited after publishing or when it has submissions because AllowEditAfterPublish is false.");
        }

        // If AllowEditAfterPublish == true -> allow edits (update version or keep audit trail)
        // For now, we'll just update the assignment properties
        if (request.Title != null)
        {
            assignment.Title = request.Title;
        }

        if (request.Description != null)
        {
            assignment.Description = request.Description;
        }

        if (request.MaxScore.HasValue)
        {
            // Business rule: ReadingMaterial type cannot be scored
            if (assignment.AssignmentType == AssignmentType.ReadingMaterial)
            {
                assignment.MaxScore = 0;
            }
            else
            {
                assignment.MaxScore = request.MaxScore.Value;
            }
        }

        if (request.Deadline.HasValue)
        {
            assignment.Deadline = request.Deadline;
        }

        if (request.AllowEditAfterPublish.HasValue)
        {
            assignment.AllowEditAfterPublish = request.AllowEditAfterPublish.Value;
        }

        if (request.AllowResubmit.HasValue)
        {
            assignment.AllowResubmit = request.AllowResubmit.Value;
        }

        assignment.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Assignments.UpdateAsync(assignment);
        await _unitOfWork.SaveChangesAsync();

        // Log audit entry
        var newValues = new
        {
            Title = assignment.Title,
            Description = assignment.Description,
            MaxScore = assignment.MaxScore,
            Deadline = assignment.Deadline,
            AllowEditAfterPublish = assignment.AllowEditAfterPublish,
            AllowResubmit = assignment.AllowResubmit
        };

        await _auditService.LogAuditAsync(
            entity: "Assignment",
            entityId: assignment.Id,
            action: "Update",
            userId: request.EditedById,
            oldValue: oldValues,
            newValue: newValues,
            description: $"Assignment '{assignment.Title}' was edited by user {request.EditedById}",
            cancellationToken);

        return assignment.Id;
    }
}

