using LMS.Application.Interfaces;
using LMS.Domain.Enums;
using MediatR;

namespace LMS.Application.Features.Learning.Queries.GetDifficultySuggestions;

public class GetDifficultySuggestionsQueryHandler : IRequestHandler<GetDifficultySuggestionsQuery, List<DifficultySuggestionDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAdaptiveLearningService _adaptiveLearningService;
    private readonly IUserRepository _userRepository;

    public GetDifficultySuggestionsQueryHandler(
        IUnitOfWork unitOfWork,
        IAdaptiveLearningService adaptiveLearningService,
        IUserRepository userRepository)
    {
        _unitOfWork = unitOfWork;
        _adaptiveLearningService = adaptiveLearningService;
        _userRepository = userRepository;
    }

    public async Task<List<DifficultySuggestionDto>> Handle(GetDifficultySuggestionsQuery request, CancellationToken cancellationToken)
    {
        // Get course instance
        var courseInstance = await _unitOfWork.CourseInstances.GetByIdAsync(request.CourseInstanceId);
        if (courseInstance == null)
        {
            throw new InvalidOperationException($"CourseInstance with ID {request.CourseInstanceId} not found.");
        }

        // Get all students in the group
        var groupUsers = await _unitOfWork.GroupUsers.ListAsync();
        var studentsInGroup = groupUsers
            .Where(gu => gu.GroupId == courseInstance.GroupId && gu.Role == GroupRole.Student)
            .Select(gu => gu.UserId)
            .ToList();

        var suggestions = new List<DifficultySuggestionDto>();

        foreach (var studentId in studentsInGroup)
        {
            try
            {
                var recommendedDifficulty = await _adaptiveLearningService.GetRecommendedDifficultyAsync(studentId, cancellationToken);
                
                var difficultyLevelName = recommendedDifficulty switch
                {
                    1 => "Easy",
                    2 => "Medium",
                    3 => "Hard",
                    _ => "Medium"
                };

                var studentName = await _userRepository.GetUserFullNameAsync(studentId) ?? "Unknown";

                suggestions.Add(new DifficultySuggestionDto
                {
                    StudentId = studentId,
                    StudentName = studentName,
                    RecommendedDifficulty = recommendedDifficulty,
                    RecommendedDifficultyName = difficultyLevelName
                });
            }
            catch (Exception)
            {
                // Log error but continue with other students
                // Could inject ILogger here if needed
                continue;
            }
        }

        return suggestions.OrderBy(s => s.StudentName).ToList();
    }
}

