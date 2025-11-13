using LMS.Application.Interfaces;
using MediatR;

namespace LMS.Application.Features.Learning.Queries.GetLearningLevel;

public class GetLearningLevelQueryHandler : IRequestHandler<GetLearningLevelQuery, LearningLevelDto>
{
    private readonly IAdaptiveLearningService _adaptiveLearningService;
    private readonly IUnitOfWork _unitOfWork;

    public GetLearningLevelQueryHandler(
        IAdaptiveLearningService adaptiveLearningService,
        IUnitOfWork unitOfWork)
    {
        _adaptiveLearningService = adaptiveLearningService;
        _unitOfWork = unitOfWork;
    }

    public async Task<LearningLevelDto> Handle(GetLearningLevelQuery request, CancellationToken cancellationToken)
    {
        var difficultyLevel = await _adaptiveLearningService.GetRecommendedDifficultyAsync(request.StudentId, cancellationToken);
        
        var difficultyLevelName = difficultyLevel switch
        {
            1 => "Easy",
            2 => "Medium",
            3 => "Hard",
            _ => "Medium"
        };

        // Get the learning level entity to get UpdatedAt
        var learningLevel = await _unitOfWork.LearningLevels.GetByStudentIdAsync(request.StudentId, cancellationToken);
        
        var updatedAt = learningLevel?.UpdatedAt ?? learningLevel?.CreatedAt ?? DateTime.UtcNow;
        
        return new LearningLevelDto
        {
            StudentId = request.StudentId,
            DifficultyLevel = difficultyLevel,
            DifficultyLevelName = difficultyLevelName,
            UpdatedAt = updatedAt
        };
    }
}

