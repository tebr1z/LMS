namespace LMS.Domain.Entities;

public class QuizTelemetry : BaseEntity
{
    public int StudentId { get; set; }
    public int QuizId { get; set; }
    public int? QuizSessionId { get; set; } // Link to QuizSession if exists
    public int SecondsActive { get; set; } // Time spent active in this session
    public string SessionId { get; set; } = string.Empty; // Unique session identifier from frontend
    public DateTime Timestamp { get; set; } = DateTime.UtcNow; // When the telemetry was recorded

    // Navigation properties
    public virtual Quiz Quiz { get; set; } = null!;
    public virtual QuizSession? QuizSession { get; set; }
}

