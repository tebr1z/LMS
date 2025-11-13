using LMS.Application.Interfaces;
using MediatR;

namespace LMS.Application.Features.AI.Queries.GetAIFeedback;

public class GetAIFeedbackQueryHandler : IRequestHandler<GetAIFeedbackQuery, AIFeedbackDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetAIFeedbackQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<AIFeedbackDto?> Handle(GetAIFeedbackQuery request, CancellationToken cancellationToken)
    {
        var feedback = await _unitOfWork.AssignmentFeedbackAI.GetLatestBySubmissionIdAsync(request.SubmissionId, cancellationToken);

        if (feedback == null)
        {
            return null;
        }

        return new AIFeedbackDto
        {
            Id = feedback.Id,
            SubmissionId = feedback.SubmissionId,
            AIComment = feedback.AIComment,
            ConfidenceScore = feedback.ConfidenceScore,
            CreatedAt = feedback.CreatedAt
        };
    }
}

