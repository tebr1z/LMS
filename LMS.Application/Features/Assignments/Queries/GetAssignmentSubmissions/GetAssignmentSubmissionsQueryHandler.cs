using AutoMapper;
using LMS.Application.DTOs.Assignments;
using LMS.Application.Interfaces;
using MediatR;

namespace LMS.Application.Features.Assignments.Queries.GetAssignmentSubmissions;

public class GetAssignmentSubmissionsQueryHandler : IRequestHandler<GetAssignmentSubmissionsQuery, IEnumerable<AssignmentSubmissionDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;
    private readonly IMapper _mapper;

    public GetAssignmentSubmissionsQueryHandler(IUnitOfWork unitOfWork, IUserRepository userRepository, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
        _mapper = mapper;
    }

    public async Task<IEnumerable<AssignmentSubmissionDto>> Handle(GetAssignmentSubmissionsQuery request, CancellationToken cancellationToken)
    {
        // Check if assignment exists
        var assignment = await _unitOfWork.Assignments.GetByIdAsync(request.AssignmentId);
        if (assignment == null)
        {
            throw new InvalidOperationException($"Assignment with ID {request.AssignmentId} not found.");
        }

        // Get all submissions for this assignment
        var submissions = await _unitOfWork.AssignmentSubmissions.GetSubmissionsByAssignmentIdAsync(request.AssignmentId);
        var submissionDtos = new List<AssignmentSubmissionDto>();

        foreach (var submission in submissions)
        {
            var dto = _mapper.Map<AssignmentSubmissionDto>(submission);
            dto.AssignmentTitle = assignment.Title;
            dto.StudentName = await _userRepository.GetUserFullNameAsync(submission.StudentId) ?? "Unknown";
            
            if (submission.EvaluatedBy.HasValue)
            {
                dto.EvaluatedByName = await _userRepository.GetUserFullNameAsync(submission.EvaluatedBy.Value) ?? "Unknown";
            }

            submissionDtos.Add(dto);
        }

        return submissionDtos;
    }
}

