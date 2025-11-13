namespace LMS.Domain.Entities;

/// <summary>
/// Live session entity for real-time lessons using WebRTC/Jitsi
/// </summary>
public class LiveSession : BaseEntity
{
    public int GroupId { get; set; }
    public int TeacherId { get; set; }
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime? EndTime { get; set; }
    public string SessionUrl { get; set; } = string.Empty; // Jitsi/WebRTC meeting URL
    public string? RecordingUrl { get; set; } // URL to recorded session if available
    public string? JwtToken { get; set; } // JWT token for meeting authentication
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual Group Group { get; set; } = null!;
}

