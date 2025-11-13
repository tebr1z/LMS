using MediatR;

namespace LMS.Application.Features.Attendance.Commands.AddAttendance;

public class AddAttendanceCommand : IRequest<int>
{
    public int GroupId { get; set; }
    public int StudentId { get; set; }
    public DateTime Date { get; set; }
    public bool Present { get; set; }
    public string? Notes { get; set; }
    public int MarkedById { get; set; } // Mentor, Teacher, or StudentOffice
}

