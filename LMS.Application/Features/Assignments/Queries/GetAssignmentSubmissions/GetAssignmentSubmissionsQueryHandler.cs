using AutoMapper;
using LMS.Application.DTOs.Assignments;
using LMS.Application.Interfaces;
using LMS.Domain.Enums;
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

        // Authorization check: Teacher/Admin/Mentor (read-only for Mentor)
        var currentUserIdClaim = request.UserId; // Assume UserId is passed in query
        var currentUser = await _userRepository.GetUserByIdAsync(currentUserIdClaim);
        if (currentUser == null)
        {
            throw new UnauthorizedAccessException("Invalid user.");
        }

        // Check if user is Teacher, Admin, or Mentor
        bool isAuthorized = currentUser.Role == UserRole.Teacher || 
                           currentUser.Role == UserRole.MasterAdmin || 
                           currentUser.Role == UserRole.Admin ||
                           currentUser.Role == UserRole.Mentor;

        // If Mentor, verify they belong to the group that has this assignment
        if (currentUser.Role == UserRole.Mentor && assignment.GroupId.HasValue)
        {
            var isMentorInGroup = await _unitOfWork.Groups.IsUserInGroupAsync(assignment.GroupId.Value, currentUserIdClaim);
            if (!isMentorInGroup)
            {
                throw new UnauthorizedAccessException("Mentor is not assigned to the group for this assignment.");
            }
        }
        else if (!isAuthorized)
        {
            throw new UnauthorizedAccessException("Only Teacher, Admin, or Mentor can view assignment submissions.");
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

