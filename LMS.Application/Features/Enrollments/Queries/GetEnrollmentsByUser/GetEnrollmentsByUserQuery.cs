using LMS.Application.DTOs.Enrollments;
using MediatR;

namespace LMS.Application.Features.Enrollments.Queries.GetEnrollmentsByUser;

public class GetEnrollmentsByUserQuery : IRequest<IEnumerable<EnrollmentDto>>
{
    public int UserId { get; set; }
}

