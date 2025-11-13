using LMS.Application.Interfaces;
using LMS.Domain.Enums;
using MediatR;

namespace LMS.Application.Features.Assignments.Commands.GradeAssignment;

public class GradeAssignmentCommandHandler : IRequestHandler<GradeAssignmentCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;

    public GradeAssignmentCommandHandler(IUnitOfWork unitOfWork, IUserRepository userRepository)
    {
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
    }

    public async Task<bool> Handle(GradeAssignmentCommand request, CancellationToken cancellationToken)
    {
        // Check if submission exists
        var submission = await _unitOfWork.AssignmentSubmissions.GetByIdAsync(request.SubmissionId);
        if (submission == null)
        {
            throw new InvalidOperationException($"Submission with ID {request.SubmissionId} not found.");
        }

        // Get assignment to validate score range and type
        var assignment = await _unitOfWork.Assignments.GetByIdAsync(submission.AssignmentId);
        if (assignment == null)
        {
            throw new InvalidOperationException($"Assignment with ID {submission.AssignmentId} not found.");
        }

        // Authorization check: Teacher/Admin only
        var evaluator = await _userRepository.GetUserByIdAsync(request.EvaluatedById);
        if (evaluator == null)
        {
            throw new UnauthorizedAccessException("Invalid user performing the action.");
        }

        if (evaluator.Role == UserRole.Mentor || evaluator.Role == UserRole.StudentOffice)
        {
            throw new UnauthorizedAccessException($"{evaluator.Role} cannot grade assignments. Only Teacher or Admin can grade assignments.");
        }
        else if (evaluator.Role != UserRole.Teacher && evaluator.Role != UserRole.MasterAdmin && evaluator.Role != UserRole.Admin)
        {
            throw new UnauthorizedAccessException("Only Teacher or Admin can grade assignments.");
        }

        // Business rule: ReadingMaterial type cannot be scored (MaxScore ignored) — mark Score null
        if (assignment.AssignmentType == AssignmentType.ReadingMaterial)
        {
            // For ReadingMaterial, set score to null regardless of input
            submission.Score = null;
            submission.Feedback = request.Feedback;
            submission.EvaluatedById = request.EvaluatedById;
            submission.EvaluatedAt = DateTime.UtcNow;
        }
        else
        {
            // Validate score range: Score (0..MaxScore)
            if (request.Score < 0 || request.Score > assignment.MaxScore)
            {
                throw new InvalidOperationException($"Score must be between 0 and {assignment.MaxScore}.");
            }

            submission.Score = request.Score;
            submission.Feedback = request.Feedback;
            submission.EvaluatedById = request.EvaluatedById;
            submission.EvaluatedAt = DateTime.UtcNow;
        }

        submission.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.AssignmentSubmissions.UpdateAsync(submission);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }
}

