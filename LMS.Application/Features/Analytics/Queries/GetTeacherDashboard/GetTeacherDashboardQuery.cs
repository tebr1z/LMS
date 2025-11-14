using MediatR;

namespace LMS.Application.Features.Analytics.Queries.GetTeacherDashboard;

public class GetTeacherDashboardQuery : IRequest<TeacherDashboardDto>
{
    public int TeacherId { get; set; }
}

public class TeacherDashboardDto
{
    public decimal AvgScore { get; set; }
    public int LateSubmissions { get; set; }
    public int ActiveGroups { get; set; }
    public List<TopStudentDto> TopStudents { get; set; } = new();
    public List<TimeDistributionDto> TimeDistribution { get; set; } = new();
}

public class TopStudentDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal AvgScore { get; set; }
}

public class TimeDistributionDto
{
    public string Day { get; set; } = string.Empty; // e.g., "Monday", "Tuesday"
    public int Minutes { get; set; }
}


