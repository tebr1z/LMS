using LMS.Application.DTOs.Assignments;
using MediatR;

namespace LMS.Application.Features.Assignments.Queries.GetAssignmentSubmissions;

public class GetAssignmentSubmissionsQuery : IRequest<IEnumerable<AssignmentSubmissionDto>>
{
    public int AssignmentId { get; set; }
    public int UserId { get; set; } // For authorization check
}

