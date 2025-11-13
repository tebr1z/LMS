using LMS.Application.Interfaces;
using LMS.Domain.Enums;
using MediatR;

namespace LMS.Application.Features.Assignments.Commands.PublishAssignment;

public class PublishAssignmentCommandHandler : IRequestHandler<PublishAssignmentCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;

    public PublishAssignmentCommandHandler(IUnitOfWork unitOfWork, IUserRepository userRepository)
    {
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
    }

    public async Task<bool> Handle(PublishAssignmentCommand request, CancellationToken cancellationToken)
    {
        // Check if assignment exists
        var assignment = await _unitOfWork.Assignments.GetByIdAsync(request.AssignmentId);
        if (assignment == null)
        {
            throw new InvalidOperationException($"Assignment with ID {request.AssignmentId} not found.");
        }

        // Authorization check: Only creator Teacher or Admin can publish
        var publishedByUser = await _userRepository.GetUserByIdAsync(request.PublishedById);
        if (publishedByUser == null)
        {
            throw new UnauthorizedAccessException("Invalid user performing the action.");
        }

        if (publishedByUser.Role == UserRole.Teacher && assignment.CreatedById != request.PublishedById)
        {
            throw new UnauthorizedAccessException("Only the creator Teacher or Admin can publish assignments.");
        }
        else if (publishedByUser.Role != UserRole.MasterAdmin && publishedByUser.Role != UserRole.Admin && publishedByUser.Role != UserRole.Teacher)
        {
            throw new UnauthorizedAccessException("Only creator Teacher or Admin can publish assignments.");
        }

        // Mark visible to students
        assignment.IsPublished = true;
        assignment.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.Assignments.UpdateAsync(assignment);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }
}

