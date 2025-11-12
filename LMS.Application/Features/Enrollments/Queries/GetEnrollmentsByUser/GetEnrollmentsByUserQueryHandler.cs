using AutoMapper;
using LMS.Application.DTOs.Enrollments;
using LMS.Application.Interfaces;
using MediatR;

namespace LMS.Application.Features.Enrollments.Queries.GetEnrollmentsByUser;

public class GetEnrollmentsByUserQueryHandler : IRequestHandler<GetEnrollmentsByUserQuery, IEnumerable<EnrollmentDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetEnrollmentsByUserQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<IEnumerable<EnrollmentDto>> Handle(GetEnrollmentsByUserQuery request, CancellationToken cancellationToken)
    {
        var enrollments = await _unitOfWork.Enrollments.GetEnrollmentsByUserAsync(request.UserId);
        
        var enrollmentDtos = enrollments.Select(e => new EnrollmentDto
        {
            Id = e.Id,
            UserId = e.UserId,
            CourseId = e.CourseId,
            CourseTitle = e.Course?.Title ?? string.Empty,
            CourseDescription = e.Course?.Description ?? string.Empty,
            EnrolledAt = e.EnrolledAt,
            CreatedAt = e.CreatedAt
        });

        return enrollmentDtos;
    }
}

