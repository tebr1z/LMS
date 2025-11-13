using MediatR;

namespace LMS.Application.Features.Attendance.Queries.GetAttendance;

public class GetAttendanceQuery : IRequest<IEnumerable<AttendanceDto>>
{
    public int GroupId { get; set; }
    public DateTime? Date { get; set; } // Optional: filter by specific date
    public int? StudentId { get; set; } // Optional: filter by student
}

public class AttendanceDto
{
    public int Id { get; set; }
    public int GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public bool Present { get; set; }
    public string? Notes { get; set; }
    public int MarkedById { get; set; }
    public string MarkedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

