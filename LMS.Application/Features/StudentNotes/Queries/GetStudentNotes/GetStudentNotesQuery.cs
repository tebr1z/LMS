using MediatR;

namespace LMS.Application.Features.StudentNotes.Queries.GetStudentNotes;

public class GetStudentNotesQuery : IRequest<IEnumerable<StudentNoteDto>>
{
    public int? StudentId { get; set; } // Optional: filter by student
    public int? GroupId { get; set; } // Optional: filter by group
    public int UserId { get; set; } // User requesting notes (for authorization)
}

public class StudentNoteDto
{
    public int Id { get; set; }
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public int GroupId { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsPrivate { get; set; }
    public bool IsImportant { get; set; }
    public int CreatedById { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

