using LMS.Application.Interfaces;
using LMS.Domain.Enums;
using LMS.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Identity;

namespace LMS.Application.Features.Assignments.Commands.GradeAssignment;

public class GradeAssignmentCommandHandler : IRequestHandler<GradeAssignmentCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserManager<ApplicationUser> _userManager;

    public GradeAssignmentCommandHandler(IUnitOfWork unitOfWork, UserManager<ApplicationUser> userManager)
    {
        _unitOfWork = unitOfWork;
        _userManager = userManager;
    }

    public async Task<bool> Handle(GradeAssignmentCommand request, CancellationToken cancellationToken)
    {
        // Check if assignment exists
        var assignment = await _unitOfWork.Assignments.GetByIdAsync(request.AssignmentId);
        if (assignment == null)
        {
            throw new InvalidOperationException($"Assignment with ID {request.AssignmentId} not found.");
        }

        // Check if submission exists and belongs to the assignment
        var submission = await _unitOfWork.AssignmentSubmissions.GetByIdAsync(request.SubmissionId);
        if (submission == null)
        {
            throw new InvalidOperationException($"Submission with ID {request.SubmissionId} not found.");
        }

        if (submission.AssignmentId != request.AssignmentId)
        {
            throw new InvalidOperationException("Submission does not belong to the specified assignment.");
        }

        // Check if evaluator exists and is a Teacher
        var evaluator = await _userManager.FindByIdAsync(request.EvaluatedBy.ToString());
        if (evaluator == null)
        {
            throw new InvalidOperationException($"Evaluator with ID {request.EvaluatedBy} not found.");
        }

        if (evaluator.Role != UserRole.Teacher && evaluator.Role != UserRole.MasterAdmin && evaluator.Role != UserRole.Admin)
        {
            throw new UnauthorizedAccessException("Only teachers, admins, or master admins can grade assignments.");
        }

        // Check if assignment type allows scoring
        if (assignment.Type == AssignmentType.ReadingMaterial)
        {
            throw new InvalidOperationException("ReadingMaterial assignments cannot be scored.");
        }

        // Update submission with score
        submission.Score = request.Score;
        submission.EvaluatedBy = request.EvaluatedBy;

        await _unitOfWork.AssignmentSubmissions.UpdateAsync(submission);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }
}

