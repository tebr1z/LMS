using MediatR;

namespace LMS.Application.Features.StudentOffice.Commands.FlagStudent;

public class FlagStudentCommand : IRequest<int>
{
    public int StudentId { get; set; }
    public string Reason { get; set; } = string.Empty; // Reason for flagging
    public string RecommendedAction { get; set; } = string.Empty; // Suggested intervention
    public int CreatedById { get; set; } // StudentOffice user creating the flag
}

