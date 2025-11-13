using MediatR;

namespace LMS.Application.Features.Learning.Queries.GetLearningLevel;

public class GetLearningLevelQuery : IRequest<LearningLevelDto>
{
    public int StudentId { get; set; }
}

public class LearningLevelDto
{
    public int StudentId { get; set; }
    public int DifficultyLevel { get; set; }
    public string DifficultyLevelName { get; set; } = string.Empty; // Easy, Medium, Hard
    public DateTime UpdatedAt { get; set; }
}


