using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using MediatR;

namespace LMS.Application.Features.Telemetry.Commands.RecordAssignmentTime;

public class RecordAssignmentTimeCommandHandler : IRequestHandler<RecordAssignmentTimeCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public RecordAssignmentTimeCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(RecordAssignmentTimeCommand request, CancellationToken cancellationToken)
    {
        // Verify assignment exists
        var assignment = await _unitOfWork.Assignments.GetByIdAsync(request.AssignmentId);
        if (assignment == null)
        {
            throw new InvalidOperationException($"Assignment with ID {request.AssignmentId} not found.");
        }

        // Try to find existing submission for aggregation
        var submission = await _unitOfWork.AssignmentSubmissions
            .GetSubmissionByAssignmentAndStudentAsync(request.AssignmentId, request.StudentId);

        // Create telemetry record
        var telemetry = new AssignmentTelemetry
        {
            StudentId = request.StudentId,
            AssignmentId = request.AssignmentId,
            SecondsActive = request.SecondsActive,
            SessionId = request.SessionId,
            Timestamp = request.Timestamp,
            SubmissionId = submission?.Id,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.AssignmentTelemetry.AddAsync(telemetry);
        await _unitOfWork.SaveChangesAsync(); // Save telemetry first

        // Aggregate total time and update submission if exists
        if (submission != null)
        {
            // Get all telemetry for this submission (now including the one we just saved)
            var allTelemetry = await _unitOfWork.AssignmentTelemetry.ListAsync();
            var submissionTelemetry = allTelemetry
                .Where(t => t.SubmissionId == submission.Id)
                .ToList();

            // Sum all secondsActive for this submission
            var totalTime = submissionTelemetry.Sum(t => t.SecondsActive);
            submission.TimeOnPageInSeconds = totalTime;
            submission.UpdatedAt = DateTime.UtcNow;
            await _unitOfWork.AssignmentSubmissions.UpdateAsync(submission);
            await _unitOfWork.SaveChangesAsync(); // Save submission update
        }

        return true;
    }
}

