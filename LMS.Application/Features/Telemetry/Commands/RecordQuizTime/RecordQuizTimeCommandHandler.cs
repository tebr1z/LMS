using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using MediatR;

namespace LMS.Application.Features.Telemetry.Commands.RecordQuizTime;

public class RecordQuizTimeCommandHandler : IRequestHandler<RecordQuizTimeCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public RecordQuizTimeCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(RecordQuizTimeCommand request, CancellationToken cancellationToken)
    {
        // Verify quiz exists
        var allQuizzes = await _unitOfWork.Quizzes.ListAsync();
        var quiz = allQuizzes.FirstOrDefault(q => q.Id == request.QuizId);
        if (quiz == null)
        {
            throw new InvalidOperationException($"Quiz with ID {request.QuizId} not found.");
        }

        // If QuizSessionId provided, verify it exists and belongs to student
        QuizSession? quizSession = null;
        if (request.QuizSessionId.HasValue)
        {
            quizSession = await _unitOfWork.QuizSessions.GetByIdAsync(request.QuizSessionId.Value);
            if (quizSession != null && quizSession.StudentId != request.StudentId)
            {
                throw new UnauthorizedAccessException("QuizSession does not belong to this student.");
            }
        }
        else
        {
            // Try to find active quiz session
            quizSession = await _unitOfWork.QuizSessions.GetActiveSessionByStudentAndQuizAsync(request.StudentId, request.QuizId);
        }

        // Create telemetry record
        var telemetry = new QuizTelemetry
        {
            StudentId = request.StudentId,
            QuizId = request.QuizId,
            QuizSessionId = quizSession?.Id,
            SecondsActive = request.SecondsActive,
            SessionId = request.SessionId,
            Timestamp = request.Timestamp,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.QuizTelemetry.AddAsync(telemetry);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }
}

