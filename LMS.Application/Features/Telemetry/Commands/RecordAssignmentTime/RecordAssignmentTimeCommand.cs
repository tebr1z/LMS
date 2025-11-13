using MediatR;

namespace LMS.Application.Features.Telemetry.Commands.RecordAssignmentTime;

public class RecordAssignmentTimeCommand : IRequest<bool>
{
    public int StudentId { get; set; }
    public int AssignmentId { get; set; }
    public int SecondsActive { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

