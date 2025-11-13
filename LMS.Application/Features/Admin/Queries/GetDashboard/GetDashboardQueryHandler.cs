using LMS.Application.Interfaces;
using LMS.Domain.Enums;
using MediatR;

namespace LMS.Application.Features.Admin.Queries.GetDashboard;

public class GetDashboardQueryHandler : IRequestHandler<GetDashboardQuery, DashboardDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;

    public GetDashboardQueryHandler(IUnitOfWork unitOfWork, IUserRepository userRepository)
    {
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
    }

    public async Task<DashboardDto> Handle(GetDashboardQuery request, CancellationToken cancellationToken)
    {
        // Get all users and group by role
        var allUsers = await _userRepository.ListAsync();
        var usersByRole = allUsers
            .GroupBy(u => u.Role)
            .ToDictionary(g => g.Key.ToString(), g => g.Count());

        var totalUsers = allUsers.Count;
        var totalStudents = allUsers.Count(u => u.Role == UserRole.Student);
        var totalTeachers = allUsers.Count(u => u.Role == UserRole.Teacher);

        // Get course statistics
        var courses = await _unitOfWork.Courses.ListAsync();
        var courseInstances = await _unitOfWork.CourseInstances.ListAsync();
        var assignments = await _unitOfWork.Assignments.ListAsync();
        var submissions = await _unitOfWork.AssignmentSubmissions.ListAsync();
        var groups = await _unitOfWork.Groups.ListAsync();

        // Active courses (course instances that are active/published)
        var activeCourses = courseInstances.Count; // You might want to add IsActive or IsPublished flag

        // Payments due (overdue invoices)
        var overdueInvoices = await _unitOfWork.Invoices.GetOverdueInvoicesAsync();
        var paymentsDue = overdueInvoices.Count;
        var paymentsDueAmount = overdueInvoices.Sum(i => i.Amount);

        // At-risk students (using existing query logic)
        var atRiskStudentsCount = 0;
        try
        {
            var students = allUsers.Where(u => u.Role == UserRole.Student).ToList();
            
            foreach (var student in students)
            {
                var riskFactors = new List<string>();

                // Check attendance
                var allAttendances = await _unitOfWork.Attendances.ListAsync();
                var studentAttendances = allAttendances
                    .Where(a => a.StudentId == student.Id)
                    .ToList();

                if (studentAttendances.Any())
                {
                    var totalSessions = studentAttendances.Count;
                    var presentCount = studentAttendances.Count(a => a.Present);
                    var attendancePercent = (decimal)presentCount / totalSessions * 100;

                    if (attendancePercent < 75)
                    {
                        riskFactors.Add("Low attendance");
                    }
                }

                // Check average score
                var stats = await _unitOfWork.StudentStats.GetByStudentIdAsync(student.Id, courseInstanceId: null);
                if (stats != null && stats.AveragePercent < 60 && stats.AssignmentsCompletedCount > 0)
                {
                    riskFactors.Add("Low average score");
                }

                // Check missing assignments
                var allAssignments = await _unitOfWork.Assignments.ListAsync();
                var allSubmissions = await _unitOfWork.AssignmentSubmissions.ListAsync();
                var now = DateTime.UtcNow;
                var pastDueAssignments = allAssignments
                    .Where(a => a.Deadline.HasValue && a.Deadline.Value < now)
                    .ToList();

                var missingAssignments = pastDueAssignments
                    .Where(a =>
                    {
                        var submission = allSubmissions
                            .FirstOrDefault(s => s.AssignmentId == a.Id && s.StudentId == student.Id);
                        return submission == null;
                    })
                    .ToList();

                if (missingAssignments.Count >= 3)
                {
                    riskFactors.Add("Missing assignments");
                }

                if (riskFactors.Any())
                {
                    atRiskStudentsCount++;
                }
            }
        }
        catch
        {
            // If there's an error calculating at-risk students, set to 0
            atRiskStudentsCount = 0;
        }

        var systemStats = new SystemStatsDto
        {
            TotalUsers = totalUsers,
            TotalStudents = totalStudents,
            TotalTeachers = totalTeachers,
            TotalCourses = courses.Count,
            TotalCourseInstances = courseInstances.Count,
            TotalAssignments = assignments.Count,
            TotalSubmissions = submissions.Count,
            TotalGroups = groups.Count,
            UsersByRole = usersByRole
        };

        return new DashboardDto
        {
            SystemStats = systemStats,
            ActiveCourses = activeCourses,
            PaymentsDue = paymentsDue,
            PaymentsDueAmount = paymentsDueAmount,
            AtRiskStudents = atRiskStudentsCount,
            LastUpdated = DateTime.UtcNow
        };
    }
}

