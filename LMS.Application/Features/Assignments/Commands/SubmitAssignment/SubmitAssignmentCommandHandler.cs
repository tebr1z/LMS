using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Domain.Enums;
using MediatR;

namespace LMS.Application.Features.Assignments.Commands.SubmitAssignment;

public class SubmitAssignmentCommandHandler : IRequestHandler<SubmitAssignmentCommand, int>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;
    private readonly IAuditService _auditService;

    public SubmitAssignmentCommandHandler(
        IUnitOfWork unitOfWork,
        IUserRepository userRepository,
        IAuditService auditService)
    {
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
        _auditService = auditService;
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
            
            // Aggregate telemetry time if not provided
            if (!request.TimeOnPageInSeconds.HasValue || request.TimeOnPageInSeconds.Value == 0)
            {
                var totalTelemetryTime = await _unitOfWork.AssignmentTelemetry.GetTotalTimeOnPageBySubmissionAsync(existingSubmission.Id);
                existingSubmission.TimeOnPageInSeconds = totalTelemetryTime > 0 ? totalTelemetryTime : existingSubmission.TimeOnPageInSeconds;
            }
            else
            {
                existingSubmission.TimeOnPageInSeconds = request.TimeOnPageInSeconds;
            }
            
            existingSubmission.SubmittedAt = now;
            existingSubmission.Score = null; // Reset score if resubmitting
            existingSubmission.Feedback = null;
            existingSubmission.EvaluatedById = null;
            existingSubmission.EvaluatedAt = null;
            existingSubmission.UpdatedAt = now;

            // Store old values for audit
            var oldSubmissionValues = new
            {
                FileUrl = existingSubmission.FileUrl,
                AnswerText = existingSubmission.AnswerText,
                SubmittedAt = existingSubmission.SubmittedAt,
                Score = existingSubmission.Score,
                Feedback = existingSubmission.Feedback
            };

            await _unitOfWork.AssignmentSubmissions.UpdateAsync(existingSubmission);
            await _unitOfWork.SaveChangesAsync();

            // Log audit entry for submission update
            var newSubmissionValues = new
            {
                FileUrl = existingSubmission.FileUrl,
                AnswerText = existingSubmission.AnswerText,
                SubmittedAt = existingSubmission.SubmittedAt,
                Score = existingSubmission.Score,
                Feedback = existingSubmission.Feedback
            };

            await _auditService.LogAuditAsync(
                entity: "Submission",
                entityId: existingSubmission.Id,
                action: "Update",
                userId: request.StudentId,
                oldValue: oldSubmissionValues,
                newValue: newSubmissionValues,
                description: $"Student {request.StudentId} resubmitted Assignment {request.AssignmentId}",
                cancellationToken);

            return existingSubmission.Id;
        }

        // Aggregate telemetry time if not provided
        int? aggregatedTime = request.TimeOnPageInSeconds;
        if (!aggregatedTime.HasValue || aggregatedTime.Value == 0)
        {
            // Try to aggregate from telemetry before submission
            var allTelemetry = await _unitOfWork.AssignmentTelemetry.ListAsync();
            var preSubmissionTelemetry = allTelemetry
                .Where(t => t.StudentId == request.StudentId && 
                           t.AssignmentId == request.AssignmentId &&
                           t.SubmissionId == null) // Telemetry not yet linked to submission
                .Sum(t => t.SecondsActive);
            
            if (preSubmissionTelemetry > 0)
            {
                aggregatedTime = preSubmissionTelemetry;
            }
        }

        // Create new submission
        var submission = new AssignmentSubmission
        {
            AssignmentId = request.AssignmentId,
            StudentId = request.StudentId,
            FileUrl = request.FileUrl,
            AnswerText = request.AnswerText,
            TimeOnPageInSeconds = aggregatedTime,
            SubmittedAt = now,
            CreatedAt = now
        };

        await _unitOfWork.AssignmentSubmissions.AddAsync(submission);
        await _unitOfWork.SaveChangesAsync(); // Save to get submission ID

        // Log audit entry for submission creation
        var submissionValues = new
        {
            AssignmentId = submission.AssignmentId,
            StudentId = submission.StudentId,
            FileUrl = submission.FileUrl,
            AnswerText = submission.AnswerText,
            SubmittedAt = submission.SubmittedAt,
            TimeOnPageInSeconds = submission.TimeOnPageInSeconds
        };

        await _auditService.LogAuditAsync(
            entity: "Submission",
            entityId: submission.Id,
            action: "Create",
            userId: request.StudentId,
            oldValue: null,
            newValue: submissionValues,
            description: $"Student {request.StudentId} submitted Assignment {request.AssignmentId}",
            cancellationToken);

        // Link pre-submission telemetry to this submission
        var allTelemetryForLinking = await _unitOfWork.AssignmentTelemetry.ListAsync();
        var preSubmissionTelemetryForLinking = allTelemetryForLinking
            .Where(t => t.StudentId == request.StudentId && 
                       t.AssignmentId == request.AssignmentId &&
                       t.SubmissionId == null)
            .ToList();

        foreach (var telemetry in preSubmissionTelemetryForLinking)
        {
            telemetry.SubmissionId = submission.Id;
            await _unitOfWork.AssignmentTelemetry.UpdateAsync(telemetry);
        }

        // Recalculate total time from all linked telemetry
        if (preSubmissionTelemetryForLinking.Any())
        {
            var totalTime = await _unitOfWork.AssignmentTelemetry.GetTotalTimeOnPageBySubmissionAsync(submission.Id);
            submission.TimeOnPageInSeconds = totalTime;
            await _unitOfWork.AssignmentSubmissions.UpdateAsync(submission);
        }

        await _unitOfWork.SaveChangesAsync();

        return submission.Id;
    }
}

