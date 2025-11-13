using MediatR;

namespace LMS.Application.Features.Learning.Queries.GetDifficultySuggestions;

/// <summary>
/// Query to get difficulty suggestions for all students in a course instance/group
/// </summary>
public class GetDifficultySuggestionsQuery : IRequest<List<DifficultySuggestionDto>>
{
    public int CourseInstanceId { get; set; }
}

public class DifficultySuggestionDto
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public int RecommendedDifficulty { get; set; }
    public string RecommendedDifficultyName { get; set; } = string.Empty; // Easy, Medium, Hard
}


