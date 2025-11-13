using MediatR;

namespace LMS.Application.Features.Telemetry.Commands.RecordQuizTime;

public class RecordQuizTimeCommand : IRequest<bool>
{
    public int StudentId { get; set; }
    public int QuizId { get; set; }
    public int SecondsActive { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int? QuizSessionId { get; set; } // Optional: link to QuizSession if exists
}

