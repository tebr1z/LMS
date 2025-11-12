namespace LMS.Domain.Entities;

public class Message : BaseEntity
{
    public int SenderId { get; set; }
    public int ReceiverId { get; set; }
    public int? GroupId { get; set; } // Optional: if set, message is a group chat message
    public string Text { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Group? Group { get; set; }
}

