using MediatR;

namespace LMS.Application.Features.AI.Queries.GetAIFeedback;

public class GetAIFeedbackQuery : IRequest<AIFeedbackDto?>
{
    public int SubmissionId { get; set; }
}

public class AIFeedbackDto
{
    public int Id { get; set; }
    public int SubmissionId { get; set; }
    public string AIComment { get; set; } = string.Empty;
    public decimal? ConfidenceScore { get; set; }
    public DateTime CreatedAt { get; set; }
}

