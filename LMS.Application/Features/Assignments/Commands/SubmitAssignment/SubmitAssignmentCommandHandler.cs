using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Domain.Enums;
using MediatR;

namespace LMS.Application.Features.Assignments.Commands.SubmitAssignment;

public class SubmitAssignmentCommandHandler : IRequestHandler<SubmitAssignmentCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;

    public SubmitAssignmentCommandHandler(IUnitOfWork unitOfWork, IUserRepository userRepository)
    {
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
    }

    public async Task<int> Handle(SubmitAssignmentCommand request, CancellationToken cancellationToken)
    {
        // Check if assignment exists
        var assignment = await _unitOfWork.Assignments.GetByIdAsync(request.AssignmentId);
        if (assignment == null)
        {
            throw new InvalidOperationException($"Assignment with ID {request.AssignmentId} not found.");
        }

        // Check if student exists and is a Student
        var student = await _userRepository.GetUserByIdAsync(request.StudentId);
        if (student == null)
        {
            throw new InvalidOperationException($"Student with ID {request.StudentId} not found.");
        }

        if (student.Role != UserRole.Student)
        {
            throw new UnauthorizedAccessException("Only students can submit assignments.");
        }

        // Check if deadline has passed (enforce deadline)
        if (assignment.Deadline != default(DateTime) && assignment.Deadline < DateTime.UtcNow)
        {
            throw new InvalidOperationException("Cannot submit assignment after deadline has passed.");
        }

        // Check if student already submitted
        var existingSubmission = await _unitOfWork.AssignmentSubmissions
            .GetSubmissionByAssignmentAndStudentAsync(request.AssignmentId, request.StudentId);
        
        if (existingSubmission != null)
        {
            // Update existing submission
            existingSubmission.FileUrl = request.FileUrl ?? existingSubmission.FileUrl;
            existingSubmission.AnswerText = request.AnswerText ?? existingSubmission.AnswerText;
            existingSubmission.SubmittedAt = DateTime.UtcNow;
            existingSubmission.Score = null; // Reset score if resubmitting
            existingSubmission.EvaluatedBy = null;

            await _unitOfWork.AssignmentSubmissions.UpdateAsync(existingSubmission);
            await _unitOfWork.SaveChangesAsync();

            return existingSubmission.Id;
        }

        // Create new submission
        var submission = new AssignmentSubmission
        {
            AssignmentId = request.AssignmentId,
            StudentId = request.StudentId,
            FileUrl = request.FileUrl,
            AnswerText = request.AnswerText,
            SubmittedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.AssignmentSubmissions.AddAsync(submission);
        await _unitOfWork.SaveChangesAsync();

        return submission.Id;
    }
}

