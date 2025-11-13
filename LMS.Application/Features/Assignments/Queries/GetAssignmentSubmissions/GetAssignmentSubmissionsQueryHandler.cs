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

        // Authorization check: Teacher/Admin only
        var currentUserIdClaim = request.UserId; // Assume UserId is passed in query
        var currentUser = await _userRepository.GetUserByIdAsync(currentUserIdClaim);
        if (currentUser == null)
        {
            throw new UnauthorizedAccessException("Invalid user.");
        }

        if (currentUser.Role != UserRole.Teacher && currentUser.Role != UserRole.MasterAdmin && currentUser.Role != UserRole.Admin)
        {
            throw new UnauthorizedAccessException("Only Teacher or Admin can view assignment submissions.");
        }

        // Get all submissions for this assignment with files & time on page & current score
        var submissions = await _unitOfWork.AssignmentSubmissions.GetSubmissionsByAssignmentIdAsync(request.AssignmentId);
        var submissionDtos = new List<AssignmentSubmissionDto>();

        foreach (var submission in submissions)
        {
            var dto = _mapper.Map<AssignmentSubmissionDto>(submission);
            dto.AssignmentTitle = assignment.Title;
            dto.StudentName = await _userRepository.GetUserFullNameAsync(submission.StudentId) ?? "Unknown";
            dto.FileUrl = submission.FileUrl;
            dto.TimeOnPageInSeconds = submission.TimeOnPageInSeconds;
            dto.Score = submission.Score;
            dto.Feedback = submission.Feedback;
            
            if (submission.EvaluatedById.HasValue)
            {
                dto.EvaluatedBy = submission.EvaluatedById.Value;
                dto.EvaluatedByName = await _userRepository.GetUserFullNameAsync(submission.EvaluatedById.Value) ?? "Unknown";
            }

            dto.EvaluatedAt = submission.EvaluatedAt;

            submissionDtos.Add(dto);
        }

        return submissionDtos;
    }
}

