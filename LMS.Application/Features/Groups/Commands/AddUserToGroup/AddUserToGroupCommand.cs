using LMS.Domain.Enums;
using MediatR;

namespace LMS.Application.Features.Groups.Commands.AddUserToGroup;

public class AddUserToGroupCommand : IRequest<int>
{
    public int GroupId { get; set; }
    public int UserId { get; set; }
    public GroupRole Role { get; set; } // GroupRole enum: Teacher, Mentor, Student, StudentOffice, Finance
    public int AddedBy { get; set; } // User who is adding (for authorization check)
}

