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
        // Check if assignment exists and is published
        var assignment = await _unitOfWork.Assignments.GetByIdAsync(request.AssignmentId);
        if (assignment == null)
        {
            throw new InvalidOperationException($"Assignment with ID {request.AssignmentId} not found.");
        }

        // Check if assignment is published
        if (!assignment.IsPublished)
        {
            throw new InvalidOperationException("Assignment is not published yet.");
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

        // Validate deadline: if Deadline exists and now > Deadline and resubmission not allowed -> reject
        var now = DateTime.UtcNow;
        if (assignment.Deadline.HasValue && now > assignment.Deadline.Value)
        {
            if (!assignment.AllowResubmit)
            {
                throw new InvalidOperationException("Cannot submit assignment after deadline has passed. Resubmission is not allowed.");
            }
            // If AllowResubmit is true, allow submission even after deadline
        }

        // Check if student already submitted
        var existingSubmission = await _unitOfWork.AssignmentSubmissions
            .GetSubmissionByAssignmentAndStudentAsync(request.AssignmentId, request.StudentId);
        
        if (existingSubmission != null)
        {
            // Business rule: AllowResubmit allows multiple submissions; otherwise only first accepted
            if (!assignment.AllowResubmit)
            {
                throw new InvalidOperationException("Assignment does not allow resubmission. Only first submission is accepted.");
            }

            // Update existing submission (resubmission allowed)
            existingSubmission.FileUrl = request.FileUrl ?? existingSubmission.FileUrl;
            existingSubmission.AnswerText = request.AnswerText ?? existingSubmission.AnswerText;
            existingSubmission.TimeOnPageInSeconds = request.TimeOnPageInSeconds ?? existingSubmission.TimeOnPageInSeconds;
            existingSubmission.SubmittedAt = now;
            existingSubmission.Score = null; // Reset score if resubmitting
            existingSubmission.Feedback = null;
            existingSubmission.EvaluatedById = null;
            existingSubmission.EvaluatedAt = null;
            existingSubmission.UpdatedAt = now;

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
            TimeOnPageInSeconds = request.TimeOnPageInSeconds,
            SubmittedAt = now,
            CreatedAt = now
        };

        await _unitOfWork.AssignmentSubmissions.AddAsync(submission);
        await _unitOfWork.SaveChangesAsync();

        return submission.Id;
    }
}

