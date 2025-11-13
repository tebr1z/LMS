using LMS.Application.Interfaces;
using MediatR;

namespace LMS.Application.Features.Quizzes.Queries.GetQuizStatisticsByStudent;

public class GetQuizStatisticsByStudentQueryHandler : IRequestHandler<GetQuizStatisticsByStudentQuery, QuizStatisticsDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;

    public GetQuizStatisticsByStudentQueryHandler(IUnitOfWork unitOfWork, IUserRepository userRepository)
    {
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
    }

    public async Task<QuizStatisticsDto> Handle(GetQuizStatisticsByStudentQuery request, CancellationToken cancellationToken)
    {
        // Get student info
        var student = await _userRepository.GetUserByIdAsync(request.StudentId);
        if (student == null)
        {
            throw new InvalidOperationException($"Student with ID {request.StudentId} not found.");
        }

        // Get quiz sessions
        List<Domain.Entities.QuizSession> sessions;
        if (request.QuizId.HasValue)
        {
            // Filter by specific quiz
            var quiz = await _unitOfWork.Quizzes.GetByIdAsync(request.QuizId.Value);
            if (quiz == null)
            {
                throw new InvalidOperationException($"Quiz with ID {request.QuizId.Value} not found.");
            }

            sessions = await _unitOfWork.QuizSessions.GetSessionsByQuizIdAsync(quiz.Id);
            sessions = sessions.Where(s => s.StudentId == request.StudentId).ToList();
        }
        else if (request.AssignmentId.HasValue)
        {
            // Filter by assignment
            sessions = await _unitOfWork.QuizSessions.GetSessionsByAssignmentIdAsync(request.AssignmentId.Value);
            sessions = sessions.Where(s => s.StudentId == request.StudentId).ToList();
        }
        else
        {
            // Get all sessions for student
            sessions = await _unitOfWork.QuizSessions.GetSessionsByStudentIdAsync(request.StudentId);
        }

        // Load sessions with responses
        var sessionDtos = new List<QuizSessionStatisticsDto>();
        foreach (var session in sessions.OrderByDescending(s => s.StartedAt))
        {
            var sessionWithResponses = await _unitOfWork.QuizSessions.GetSessionWithResponsesAsync(session.Id);
            if (sessionWithResponses == null) continue;

            var quiz = await _unitOfWork.Quizzes.GetQuizWithQuestionsAsync(sessionWithResponses.QuizId);
            var assignment = await _unitOfWork.Assignments.GetByIdAsync(sessionWithResponses.AssignmentId);

            if (quiz == null || assignment == null) continue;

            sessionDtos.Add(new QuizSessionStatisticsDto
            {
                SessionId = sessionWithResponses.Id,
                QuizId = quiz.Id,
                QuizName = assignment.Title,
                AssignmentId = assignment.Id,
                AssignmentTitle = assignment.Title,
                StartedAt = sessionWithResponses.StartedAt,
                EndedAt = sessionWithResponses.EndedAt,
                IsCompleted = sessionWithResponses.IsCompleted,
                TotalPoints = sessionWithResponses.TotalPoints,
                PointsAwarded = sessionWithResponses.PointsAwarded,
                PercentageScore = sessionWithResponses.PercentageScore,
                Score = sessionWithResponses.Score,
                MaxScore = assignment.MaxScore,
                Passed = sessionWithResponses.Passed,
                QuestionsAnswered = sessionWithResponses.Responses?.Count ?? 0,
                TotalQuestions = quiz.Questions.Count
            });
        }

        // Calculate overall statistics
        var completedSessions = sessionDtos.Where(s => s.IsCompleted).ToList();
        var overall = new QuizOverallStatisticsDto
        {
            TotalQuizzesTaken = sessionDtos.Count,
            CompletedQuizzes = completedSessions.Count,
            PassedQuizzes = completedSessions.Count(s => s.Passed == true),
            AveragePercentageScore = completedSessions.Any() 
                ? completedSessions.Average(s => (double)s.PercentageScore) 
                : 0,
            AverageScore = completedSessions.Any()
                ? completedSessions.Average(s => (double)(s.Score ?? 0))
                : 0
        };

        var studentName = await _userRepository.GetUserFullNameAsync(request.StudentId) ?? "Unknown";

        return new QuizStatisticsDto
        {
            StudentId = request.StudentId,
            StudentName = studentName,
            Sessions = sessionDtos,
            Overall = overall
        };
    }
}

