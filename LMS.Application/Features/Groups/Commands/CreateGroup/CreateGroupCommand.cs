using MediatR;

namespace LMS.Application.Features.Groups.Commands.CreateGroup;

public class CreateGroupCommand : IRequest<int>
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CreatedById { get; set; }
}

