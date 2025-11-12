using MediatR;

namespace LMS.Application.Features.Groups.Commands.CreateGroup;

public class CreateGroupCommand : IRequest<int>
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int CreatedBy { get; set; }
}

