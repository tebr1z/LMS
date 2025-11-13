using LMS.Application.Interfaces;
using LMS.Domain.Enums;
using MediatR;

namespace LMS.Application.Features.Analytics.Queries.GetTopEngagedStudents;

public class GetTopEngagedStudentsQueryHandler : IRequestHandler<GetTopEngagedStudentsQuery, IEnumerable<EngagedStudentDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;

    public GetTopEngagedStudentsQueryHandler(IUnitOfWork unitOfWork, IUserRepository userRepository)
    {
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
    }

    public async Task<IEnumerable<EngagedStudentDto>> Handle(GetTopEngagedStudentsQuery request, CancellationToken cancellationToken)
    {
        // Get all students
        var allUsers = await _userRepository.ListAsync();
        var students = allUsers.Where(u => u.Role == UserRole.Student).ToList();

        var engagedStudents = new List<EngagedStudentDto>();

        // Determine date range
        var fromDate = request.FromDate ?? DateTime.UtcNow.AddMonths(-6); // Default: last 6 months
        var toDate = request.ToDate ?? DateTime.UtcNow;

        foreach (var student in students)
        {
            // Get assignments for this student (within course if specified)
            var allAssignments = await _unitOfWork.Assignments.ListAsync();
            var studentAssignments = allAssignments.Where(a =>
            {
                if (request.CourseId.HasValue && a.CourseId != request.CourseId.Value)
                    return false;

                // Check date range if needed (based on assignment creation or submission)
                return true;
            }).ToList();

            // Get student's submissions
            var allSubmissions = await _unitOfWork.AssignmentSubmissions.ListAsync();
            var studentSubmissions = allSubmissions
                .Where(s => s.StudentId == student.Id &&
                           (!request.CourseId.HasValue || studentAssignments.Any(a => a.Id == s.AssignmentId && a.CourseId == request.CourseId.Value)))
                .ToList();

            // Calculate total time on page (aggregate from telemetry or submissions)
            var totalTimeOnPageInSeconds = 0;
            if (studentSubmissions.Any())
            {
                foreach (var submission in studentSubmissions)
                {
                    var timeFromSubmission = submission.TimeOnPageInSeconds ?? 0;
                    
                    // Also get time from telemetry
                    var telemetryTime = await _unitOfWork.AssignmentTelemetry
                        .GetTotalTimeOnPageBySubmissionAsync(submission.Id);
                    
                    totalTimeOnPageInSeconds += Math.Max(timeFromSubmission, telemetryTime);
                }
            }
            else
            {
                // Calculate from telemetry directly if no submission yet
                var allTelemetry = await _unitOfWork.AssignmentTelemetry.ListAsync();
                var studentTelemetry = allTelemetry
                    .Where(t => t.StudentId == student.Id &&
                               t.Timestamp >= fromDate &&
                               t.Timestamp <= toDate &&
                               (!request.CourseId.HasValue || studentAssignments.Any(a => a.Id == t.AssignmentId && a.CourseId == request.CourseId.Value)))
                    .ToList();

                totalTimeOnPageInSeconds = studentTelemetry.Sum(t => t.SecondsActive);
            }

            // Get completed assignments count
            var completedAssignments = studentSubmissions
                .Where(s => s.Score.HasValue || !string.IsNullOrEmpty(s.AnswerText) || !string.IsNullOrEmpty(s.FileUrl))
                .Count();

            // Get quiz statistics
            var allQuizSessions = await _unitOfWork.QuizSessions.ListAsync();
            var studentQuizSessions = allQuizSessions
                .Where(qs => qs.StudentId == student.Id &&
                           qs.StartedAt >= fromDate &&
                           qs.StartedAt <= toDate)
                .ToList();

            var quizAttempts = studentQuizSessions.Count;
            var quizPasses = studentQuizSessions.Count(qs => qs.Passed == true);
            var quizPassRate = quizAttempts > 0 ? (decimal)quizPasses / quizAttempts * 100 : 0;

            // Calculate average score
            var scoredSubmissions = studentSubmissions.Where(s => s.Score.HasValue).ToList();
            var averageScore = scoredSubmissions.Any()
                ? scoredSubmissions.Average(s => (double)(s.Score ?? 0))
                : 0;

            // Only include students with activity
            if (totalTimeOnPageInSeconds > 0 || completedAssignments > 0 || quizAttempts > 0)
            {
                engagedStudents.Add(new EngagedStudentDto
                {
                    StudentId = student.Id,
                    StudentName = await _userRepository.GetUserFullNameAsync(student.Id) ?? "Unknown",
                    TotalTimeOnPageInSeconds = totalTimeOnPageInSeconds,
                    TotalTimeOnPageInHours = Math.Round(totalTimeOnPageInSeconds / 3600m, 2),
                    TotalAssignmentsCompleted = completedAssignments,
                    TotalQuizzesAttempted = quizAttempts,
                    TotalQuizzesPassed = quizPasses,
                    QuizPassRate = Math.Round(quizPassRate, 2),
                    AverageScore = Math.Round((decimal)averageScore, 2),
                    EngagementScore = new EngagementScoreDto
                    {
                        // Calculate engagement score (normalized to 0-100)
                        // These would be calculated based on class averages or thresholds
                        TimeScore = 0, // To be calculated
                        ActivityScore = 0, // To be calculated
                        PerformanceScore = 0, // To be calculated
                        OverallScore = 0 // To be calculated
                    }
                });
            }
        }

        // Calculate engagement scores (normalize based on max values)
        if (engagedStudents.Any())
        {
            var maxTime = engagedStudents.Max(s => s.TotalTimeOnPageInSeconds);
            var maxAssignments = engagedStudents.Max(s => s.TotalAssignmentsCompleted);
            var maxQuizzes = engagedStudents.Max(s => s.TotalQuizzesAttempted);
            var maxScore = engagedStudents.Max(s => s.AverageScore);

            foreach (var student in engagedStudents)
            {
                student.EngagementScore.TimeScore = maxTime > 0 
                    ? Math.Round((decimal)student.TotalTimeOnPageInSeconds / maxTime * 100, 2) 
                    : 0;

                student.EngagementScore.ActivityScore = Math.Max(maxAssignments, maxQuizzes) > 0
                    ? Math.Round((decimal)(student.TotalAssignmentsCompleted + student.TotalQuizzesAttempted) / Math.Max(maxAssignments, maxQuizzes) * 100, 2)
                    : 0;

                student.EngagementScore.PerformanceScore = maxScore > 0
                    ? Math.Round(student.AverageScore / maxScore * 100, 2)
                    : 0;

                // Overall score: weighted average (Time: 30%, Activity: 40%, Performance: 30%)
                student.EngagementScore.OverallScore = Math.Round(
                    student.EngagementScore.TimeScore * 0.3m +
                    student.EngagementScore.ActivityScore * 0.4m +
                    student.EngagementScore.PerformanceScore * 0.3m, 2);
            }
        }

        // Sort by overall engagement score and return top N
        var topStudents = engagedStudents
            .OrderByDescending(s => s.EngagementScore.OverallScore)
            .ThenByDescending(s => s.TotalTimeOnPageInSeconds)
            .ThenByDescending(s => s.QuizPassRate)
            .Take(request.TopN ?? 10)
            .ToList();

        return topStudents;
    }
}

