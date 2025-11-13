using MediatR;

namespace LMS.Application.Features.StudentNotes.Commands.AddStudentNote;

public class AddStudentNoteCommand : IRequest<int>
{
    public int StudentId { get; set; }
    public int GroupId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsPrivate { get; set; } = true; // Private to StudentOffice & assigned Teachers
    public bool IsImportant { get; set; } = false;
    public int CreatedById { get; set; } // Mentor, Teacher, or StudentOffice
}

