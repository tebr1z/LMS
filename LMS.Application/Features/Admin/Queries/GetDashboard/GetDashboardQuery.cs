using MediatR;

namespace LMS.Application.Features.Admin.Queries.GetDashboard;

public class GetDashboardQuery : IRequest<DashboardDto>
{
}

public class DashboardDto
{
    public SystemStatsDto SystemStats { get; set; } = new();
    public int ActiveCourses { get; set; }
    public int PaymentsDue { get; set; }
    public decimal PaymentsDueAmount { get; set; }
    public int AtRiskStudents { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public class SystemStatsDto
{
    public int TotalUsers { get; set; }
    public int TotalStudents { get; set; }
    public int TotalTeachers { get; set; }
    public int TotalCourses { get; set; }
    public int TotalCourseInstances { get; set; }
    public int TotalAssignments { get; set; }
    public int TotalSubmissions { get; set; }
    public int TotalGroups { get; set; }
    public Dictionary<string, int> UsersByRole { get; set; } = new();
}

