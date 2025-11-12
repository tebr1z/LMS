using AutoMapper;
using LMS.Application.DTOs.Assignments;
using LMS.Application.Interfaces;
using MediatR;

namespace LMS.Application.Features.Assignments.Queries.GetAssignmentsByCourse;

public class GetAssignmentsByCourseQueryHandler : IRequestHandler<GetAssignmentsByCourseQuery, IEnumerable<AssignmentDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public GetAssignmentsByCourseQueryHandler(IUnitOfWork unitOfWork, IUserRepository userRepository, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<AssignmentDto>> Handle(GetAssignmentsByCourseQuery request, CancellationToken cancellationToken)
    {
        // Check if course exists
        var course = await _unitOfWork.Courses.GetByIdAsync(request.CourseId);
        if (course == null)
        {
            throw new InvalidOperationException($"Course with ID {request.CourseId} not found.");
        }

        var assignments = await _unitOfWork.Assignments.GetAssignmentsByCourseIdAsync(request.CourseId);
        var assignmentDtos = new List<AssignmentDto>();

        foreach (var assignment in assignments)
        {
            var dto = _mapper.Map<AssignmentDto>(assignment);
            dto.CourseTitle = course.Title;
            dto.CreatedByName = await _userRepository.GetUserFullNameAsync(assignment.CreatedBy) ?? "Unknown";
            dto.TypeName = assignment.Type.ToString();
            dto.IsDeadlinePassed = assignment.Deadline < DateTime.UtcNow;
            dto.SubmissionCount = assignment.Submissions?.Count ?? 0;
            assignmentDtos.Add(dto);
        }

        return assignmentDtos;
    }
}

