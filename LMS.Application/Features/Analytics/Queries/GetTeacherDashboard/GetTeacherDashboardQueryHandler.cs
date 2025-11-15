using LMS.Application.Interfaces;
using LMS.Domain.Enums;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LMS.Application.Features.Analytics.Queries.GetTeacherDashboard;

public class GetTeacherDashboardQueryHandler : IRequestHandler<GetTeacherDashboardQuery, TeacherDashboardDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GetTeacherDashboardQueryHandler> _logger;

    public GetTeacherDashboardQueryHandler(
        IUnitOfWork unitOfWork,
        IUserRepository userRepository,
        ILogger<GetTeacherDashboardQueryHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<TeacherDashboardDto> Handle(GetTeacherDashboardQuery request, CancellationToken cancellationToken)
    {
        // Get all assignments created by the teacher
        var allAssignments = await _unitOfWork.Assignments.ListAsync();
        var teacherAssignments = allAssignments
            .Where(a => a.CreatedById == request.TeacherId)
            .ToList();

        // Get all submissions for teacher's assignments
        var allSubmissions = await _unitOfWork.AssignmentSubmissions.ListAsync();
        var teacherSubmissions = allSubmissions
            .Where(s => teacherAssignments.Any(a => a.Id == s.AssignmentId))
            .ToList();

        // Calculate average score (from submissions with scores)
        var scoredSubmissions = teacherSubmissions
            .Where(s => s.Score.HasValue)
            .ToList();

        decimal avgScore = 0;
        if (scoredSubmissions.Any())
        {
            avgScore = (decimal)scoredSubmissions.Average(s => s.PercentageScore);
        }

        // Count late submissions (submissions where SubmittedAt > Deadline)
        int lateSubmissions = 0;
        foreach (var submission in teacherSubmissions)
        {
            var assignment = teacherAssignments.FirstOrDefault(a => a.Id == submission.AssignmentId);
            if (assignment?.Deadline.HasValue == true && submission.SubmittedAt > assignment.Deadline.Value)
            {
                lateSubmissions++;
            }
        }

        // Get active groups for the teacher (groups where teacher is assigned)
        var allGroupUsers = await _unitOfWork.GroupUsers.ListAsync();
        var teacherGroupUsers = allGroupUsers
            .Where(gu => gu.UserId == request.TeacherId && gu.Role == GroupRole.Teacher)
            .Select(gu => gu.GroupId)
            .Distinct()
            .ToList();

        // Get active groups (groups that have at least one course instance)
        var allCourseInstances = await _unitOfWork.CourseInstances.ListAsync();
        var activeGroupIds = allCourseInstances
            .Select(ci => ci.GroupId)
            .Distinct()
            .ToList();

        var activeGroups = teacherGroupUsers
            .Count(gid => activeGroupIds.Contains(gid));

        // Calculate top students (from submissions, calculate average score per student)
        var allUsers = await _userRepository.ListAsync();
        var studentSubmissions = teacherSubmissions
            .Where(s => s.Score.HasValue)
            .GroupBy(s => s.StudentId)
            .Select(g => new
            {
                StudentId = g.Key,
                AvgScore = (decimal)g.Average(s => s.PercentageScore),
                SubmissionCount = g.Count()
            })
            .OrderByDescending(x => x.AvgScore)
            .Take(10) // Top 10 students
            .ToList();

        var topStudents = studentSubmissions
            .Select(s =>
            {
                var user = allUsers.FirstOrDefault(u => u.Id == s.StudentId);
                return new TopStudentDto
                {
                    Id = s.StudentId,
                    Name = user?.FullName ?? "Unknown",
                    AvgScore = s.AvgScore
                };
            })
            .ToList();

        // Calculate time distribution (based on attendance records for teacher's groups)
        // Get attendance records for students in teacher's groups
        var allAttendances = await _unitOfWork.Attendances.ListAsync();

        // Get student IDs in teacher's groups
        var teacherGroupIds = allGroupUsers
            .Where(gu => gu.UserId == request.TeacherId && gu.Role == GroupRole.Teacher)
            .Select(gu => gu.GroupId)
            .ToList();

        var studentsInTeacherGroups = allGroupUsers
            .Where(gu => teacherGroupIds.Contains(gu.GroupId) && gu.Role == GroupRole.Student)
            .Select(gu => gu.UserId)
            .Distinct()
            .ToList();

        // Get attendance records for these students in teacher's groups
        var relevantAttendances = allAttendances
            .Where(a => studentsInTeacherGroups.Contains(a.StudentId) && teacherGroupIds.Contains(a.GroupId))
            .ToList();

        // Calculate time distribution by day of week
        // DayOfWeek enum: Sunday=0, Monday=1, Tuesday=2, ..., Saturday=6
        // We want: Monday=0, Tuesday=1, ..., Sunday=6
        var timeDistribution = new List<TimeDistributionDto>();
        var daysOfWeek = new[] { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
        
        // Map our day order (Monday=0) to .NET DayOfWeek enum
        var dayOfWeekMapping = new Dictionary<int, DayOfWeek>
        {
            { 0, DayOfWeek.Monday },    // Monday
            { 1, DayOfWeek.Tuesday },   // Tuesday
            { 2, DayOfWeek.Wednesday }, // Wednesday
            { 3, DayOfWeek.Thursday },  // Thursday
            { 4, DayOfWeek.Friday },    // Friday
            { 5, DayOfWeek.Saturday },  // Saturday
            { 6, DayOfWeek.Sunday }     // Sunday
        };

        // Calculate time distribution from attendance records
        if (relevantAttendances.Any())
        {
            for (int i = 0; i < daysOfWeek.Length; i++)
            {
                var day = daysOfWeek[i];
                var targetDayOfWeek = dayOfWeekMapping[i];

                var dayAttendances = relevantAttendances
                    .Where(a => a.Date.DayOfWeek == targetDayOfWeek)
                    .Count();

                // Assuming 60 minutes per attendance/class session
                int minutes = dayAttendances * 60;

                timeDistribution.Add(new TimeDistributionDto
                {
                    Day = day,
                    Minutes = minutes
                });
            }
        }
        else
        {
            // If no attendance data, use assignment submissions as fallback
            // Calculate time based on submission day
            for (int i = 0; i < daysOfWeek.Length; i++)
            {
                var day = daysOfWeek[i];
                var targetDayOfWeek = dayOfWeekMapping[i];

                var daySubmissions = teacherSubmissions
                    .Count(s => s.SubmittedAt.DayOfWeek == targetDayOfWeek);

                // Approximate 30 minutes per submission
                int minutes = daySubmissions * 30;

                timeDistribution.Add(new TimeDistributionDto
                {
                    Day = day,
                    Minutes = minutes
                });
            }
        }

        return new TeacherDashboardDto
        {
            AvgScore = Math.Round(avgScore, 2),
            LateSubmissions = lateSubmissions,
            ActiveGroups = activeGroups,
            TopStudents = topStudents,
            TimeDistribution = timeDistribution
        };
    }
}

