using LMS.Application.DTOs.Groups;
using MediatR;

namespace LMS.Application.Features.Groups.Queries.GetGroupDetails;

public class GetGroupDetailsQuery : IRequest<GroupDto>
{
    public int GroupId { get; set; }
    public int UserId { get; set; }
    public string UserRole { get; set; } = string.Empty;
}

