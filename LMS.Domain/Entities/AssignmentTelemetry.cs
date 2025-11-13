namespace LMS.Domain.Entities;

public class AssignmentTelemetry : BaseEntity
{
    public int StudentId { get; set; }
    public int AssignmentId { get; set; }
    public int SecondsActive { get; set; } // Time spent active in this session
    public string SessionId { get; set; } = string.Empty; // Unique session identifier from frontend
    public DateTime Timestamp { get; set; } = DateTime.UtcNow; // When the telemetry was recorded
    public int? SubmissionId { get; set; } // Link to submission if exists

    // Navigation properties
    public virtual Assignment Assignment { get; set; } = null!;
    public virtual AssignmentSubmission? Submission { get; set; }
}

