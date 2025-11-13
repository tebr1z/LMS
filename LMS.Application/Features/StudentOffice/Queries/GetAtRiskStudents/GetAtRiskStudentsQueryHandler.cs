using LMS.Application.Interfaces;
using LMS.Domain.Enums;
using MediatR;

namespace LMS.Application.Features.StudentOffice.Queries.GetAtRiskStudents;

public class GetAtRiskStudentsQueryHandler : IRequestHandler<GetAtRiskStudentsQuery, IEnumerable<AtRiskStudentDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;

    public GetAtRiskStudentsQueryHandler(IUnitOfWork unitOfWork, IUserRepository userRepository)
    {
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
    }

    public async Task<IEnumerable<AtRiskStudentDto>> Handle(GetAtRiskStudentsQuery request, CancellationToken cancellationToken)
    {
        // Get all students
        var allUsers = await _userRepository.ListAsync();
        var students = allUsers.Where(u => u.Role == UserRole.Student).ToList();

        var atRiskStudents = new List<AtRiskStudentDto>();

        foreach (var student in students)
        {
            var riskFactors = new List<string>();
            var studentGroups = await _unitOfWork.GroupUsers.ListAsync();
            var studentGroupIds = studentGroups
                .Where(gu => gu.UserId == student.Id && gu.Role == GroupRole.Student)
                .Select(gu => gu.GroupId)
                .ToList();

            if (!studentGroupIds.Any()) continue;

            // Get all assignments for student's groups
            var allAssignments = await _unitOfWork.Assignments.ListAsync();
            var studentAssignments = allAssignments
                .Where(a => a.GroupId.HasValue && studentGroupIds.Contains(a.GroupId.Value))
                .ToList();

            // Get all submissions for this student
            var allSubmissions = await _unitOfWork.AssignmentSubmissions.ListAsync();
            var studentSubmissions = allSubmissions
                .Where(s => s.StudentId == student.Id)
                .ToList();

            // Calculate average score
            var scoredSubmissions = studentSubmissions
                .Where(s => s.Score.HasValue)
                .ToList();

            var averageScore = scoredSubmissions.Any()
                ? (decimal)scoredSubmissions.Average(s => (double)(s.Score ?? 0))
                : 0;

            // Count missed deadlines
            var missedDeadlines = studentAssignments
                .Where(a => a.Deadline.HasValue && 
                           a.Deadline.Value < DateTime.UtcNow &&
                           !studentSubmissions.Any(s => s.AssignmentId == a.Id && s.SubmittedAt <= a.Deadline.Value))
                .Count();

            // Calculate attendance percentage (last 30 days)
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            var allAttendances = await _unitOfWork.Attendances.ListAsync();
            var recentAttendances = allAttendances
                .Where(a => a.StudentId == student.Id && a.Date >= thirtyDaysAgo && studentGroupIds.Contains(a.GroupId))
                .ToList();

            var attendancePercentage = 0m;
            if (recentAttendances.Any())
            {
                var presentCount = recentAttendances.Count(a => a.Present);
                attendancePercentage = (decimal)presentCount / recentAttendances.Count * 100;
            }

            // Calculate progress metrics
            var progressMetrics = await CalculateProgressMetrics(student.Id, studentSubmissions, studentGroupIds);

            // Check risk factors
            if (averageScore < (request.ScoreAverageThreshold ?? 60) && scoredSubmissions.Any())
            {
                riskFactors.Add($"Low average score: {averageScore:F1}%");
            }

            if (missedDeadlines > (request.MissedDeadlinesThreshold ?? 3))
            {
                riskFactors.Add($"Missed {missedDeadlines} assignment deadlines");
            }

            if (attendancePercentage < (request.AttendanceThreshold ?? 70) && recentAttendances.Any())
            {
                riskFactors.Add($"Low attendance: {attendancePercentage:F1}%");
            }

            // Only include students with at least one risk factor
            if (riskFactors.Any())
            {
                atRiskStudents.Add(new AtRiskStudentDto
                {
                    StudentId = student.Id,
                    StudentName = await _userRepository.GetUserFullNameAsync(student.Id) ?? "Unknown",
                    RiskFactors = riskFactors,
                    AverageScore = averageScore,
                    MissedDeadlinesCount = missedDeadlines,
                    AttendancePercentage = attendancePercentage,
                    TotalAssignments = studentAssignments.Count,
                    CompletedAssignments = studentSubmissions.Count,
                    ProgressMetrics = progressMetrics
                });
            }
        }

        return atRiskStudents.OrderByDescending(s => s.RiskFactors.Count)
                            .ThenByDescending(s => s.MissedDeadlinesCount);
    }

    private async Task<StudentProgressMetricsDto> CalculateProgressMetrics(int studentId, List<Domain.Entities.AssignmentSubmission> submissions, List<int> groupIds)
    {
        // Calculate average time on page
        var submissionsWithTime = submissions.Where(s => s.TimeOnPageInSeconds.HasValue).ToList();
        var averageTimeOnPage = submissionsWithTime.Any()
            ? submissionsWithTime.Average(s => s.TimeOnPageInSeconds!.Value)
            : 0;

        // Calculate average submission lateness (in hours)
        var lateSubmissions = new List<double>();
        foreach (var submission in submissions)
        {
            var assignment = await _unitOfWork.Assignments.GetByIdAsync(submission.AssignmentId);
            if (assignment?.Deadline.HasValue == true && submission.SubmittedAt > assignment.Deadline.Value)
            {
                var hoursLate = (submission.SubmittedAt - assignment.Deadline.Value).TotalHours;
                lateSubmissions.Add(hoursLate);
            }
        }

        var averageSubmissionLateness = lateSubmissions.Any()
            ? (decimal)lateSubmissions.Average()
            : 0;

        // Calculate quiz pass rate
        var allQuizSessions = await _unitOfWork.QuizSessions.ListAsync();
        var studentQuizSessions = allQuizSessions
            .Where(qs => qs.StudentId == studentId && qs.IsCompleted)
            .ToList();

        var passedQuizzes = studentQuizSessions.Count(qs => qs.Passed == true);
        var quizPassRate = studentQuizSessions.Any()
            ? (decimal)passedQuizzes / studentQuizSessions.Count * 100
            : 0;

        return new StudentProgressMetricsDto
        {
            AverageTimeOnPage = (decimal)averageTimeOnPage,
            AverageSubmissionLateness = averageSubmissionLateness,
            QuizPassRate = quizPassRate,
            TotalQuizzes = studentQuizSessions.Count,
            PassedQuizzes = passedQuizzes
        };
    }
}

