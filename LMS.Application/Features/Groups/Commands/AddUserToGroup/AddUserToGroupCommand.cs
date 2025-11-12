using MediatR;

namespace LMS.Application.Features.Groups.Commands.AddUserToGroup;

public class AddUserToGroupCommand : IRequest<int>
{
    public int GroupId { get; set; }
    public int UserId { get; set; }
    public string Role { get; set; } = "Member"; // Leader, Member, Moderator
    public int AddedBy { get; set; } // User who is adding (for authorization check)
}

