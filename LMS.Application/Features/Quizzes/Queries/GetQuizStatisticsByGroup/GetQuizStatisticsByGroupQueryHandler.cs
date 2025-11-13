using LMS.Application.Interfaces;
using MediatR;

namespace LMS.Application.Features.Quizzes.Queries.GetQuizStatisticsByGroup;

public class GetQuizStatisticsByGroupQueryHandler : IRequestHandler<GetQuizStatisticsByGroupQuery, GroupQuizStatisticsDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;

    public GetQuizStatisticsByGroupQueryHandler(IUnitOfWork unitOfWork, IUserRepository userRepository)
    {
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
    }

    public async Task<GroupQuizStatisticsDto> Handle(GetQuizStatisticsByGroupQuery request, CancellationToken cancellationToken)
    {
        // Get group info
        var group = await _unitOfWork.Groups.GetByIdAsync(request.GroupId);
        if (group == null)
        {
            throw new InvalidOperationException($"Group with ID {request.GroupId} not found.");
        }

        // Get all students in group
        var groupUsers = await _unitOfWork.GroupUsers.GetGroupUsersByGroupIdAsync(request.GroupId);
        var studentIds = groupUsers
            .Where(gu => gu.Role == Domain.Enums.UserRole.Student)
            .Select(gu => gu.UserId)
            .ToList();

        if (!studentIds.Any())
        {
            return new GroupQuizStatisticsDto
            {
                GroupId = request.GroupId,
                GroupName = group.Name,
                Students = new List<StudentQuizStatisticsDto>(),
                Overall = new GroupOverallStatisticsDto
                {
                    TotalStudents = 0
                }
            };
        }

        // Get quiz sessions for all students
        List<Domain.Entities.QuizSession> allSessions;
        if (request.QuizId.HasValue)
        {
            var quiz = await _unitOfWork.Quizzes.GetByIdAsync(request.QuizId.Value);
            if (quiz == null)
            {
                throw new InvalidOperationException($"Quiz with ID {request.QuizId.Value} not found.");
            }

            allSessions = await _unitOfWork.QuizSessions.GetSessionsByQuizIdAsync(quiz.Id);
            allSessions = allSessions.Where(s => studentIds.Contains(s.StudentId)).ToList();
        }
        else if (request.AssignmentId.HasValue)
        {
            allSessions = await _unitOfWork.QuizSessions.GetSessionsByAssignmentIdAsync(request.AssignmentId.Value);
            allSessions = allSessions.Where(s => studentIds.Contains(s.StudentId)).ToList();
        }
        else
        {
            // Get all sessions for all students in group
            allSessions = new List<Domain.Entities.QuizSession>();
            foreach (var studentId in studentIds)
            {
                var studentSessions = await _unitOfWork.QuizSessions.GetSessionsByStudentIdAsync(studentId);
                allSessions.AddRange(studentSessions);
            }
        }

        // Group sessions by student
        var studentStatistics = new List<StudentQuizStatisticsDto>();
        foreach (var studentId in studentIds)
        {
            var studentSessions = allSessions
                .Where(s => s.StudentId == studentId)
                .OrderByDescending(s => s.StartedAt)
                .ToList();

            if (!studentSessions.Any()) continue;

            var sessionDtos = new List<QuizSessionStatisticsDto>();
            foreach (var session in studentSessions)
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

            var completedSessions = sessionDtos.Where(s => s.IsCompleted).ToList();
            var studentName = await _userRepository.GetUserFullNameAsync(studentId) ?? "Unknown";

            studentStatistics.Add(new StudentQuizStatisticsDto
            {
                StudentId = studentId,
                StudentName = studentName,
                SessionsCount = sessionDtos.Count,
                CompletedSessionsCount = completedSessions.Count,
                PassedSessionsCount = completedSessions.Count(s => s.Passed == true),
                AveragePercentageScore = completedSessions.Any()
                    ? completedSessions.Average(s => (double)s.PercentageScore)
                    : 0,
                AverageScore = completedSessions.Any()
                    ? completedSessions.Average(s => (double)(s.Score ?? 0))
                    : 0,
                Sessions = sessionDtos
            });
        }

        // Calculate overall statistics
        var allCompletedSessions = studentStatistics
            .SelectMany(s => s.Sessions.Where(ss => ss.IsCompleted))
            .ToList();

        var overall = new GroupOverallStatisticsDto
        {
            TotalStudents = studentIds.Count,
            StudentsWhoTookQuiz = studentStatistics.Count,
            StudentsWhoCompleted = studentStatistics.Count(s => s.CompletedSessionsCount > 0),
            StudentsWhoPassed = studentStatistics.Count(s => s.PassedSessionsCount > 0),
            AveragePercentageScore = allCompletedSessions.Any()
                ? allCompletedSessions.Average(s => (double)s.PercentageScore)
                : 0,
            AverageScore = allCompletedSessions.Any()
                ? allCompletedSessions.Average(s => (double)(s.Score ?? 0))
                : 0
        };

        return new GroupQuizStatisticsDto
        {
            GroupId = request.GroupId,
            GroupName = group.Name,
            Students = studentStatistics,
            Overall = overall
        };
    }
}

