using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using MediatR;

namespace LMS.Application.Features.Gamification.Commands.RedeemItem;

public class RedeemItemCommandHandler : IRequestHandler<RedeemItemCommand, RedeemItemResultDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAchievementEngine _achievementEngine;

    public RedeemItemCommandHandler(IUnitOfWork unitOfWork, IAchievementEngine achievementEngine)
    {
        _unitOfWork = unitOfWork;
        _achievementEngine = achievementEngine;
    }

    public async Task<RedeemItemResultDto> Handle(RedeemItemCommand request, CancellationToken cancellationToken)
    {
        // Get redeemable item
        var item = await _unitOfWork.RedeemableItems.GetByIdAsync(request.RedeemableItemId);
        if (item == null)
        {
            return new RedeemItemResultDto
            {
                Success = false,
                Message = "Redeemable item not found."
            };
        }

        if (!item.IsActive)
        {
            return new RedeemItemResultDto
            {
                Success = false,
                Message = "This item is no longer available for redemption."
            };
        }

        // Get user's total points
        var totalPoints = await _unitOfWork.RewardPoints.GetTotalPointsAsync(request.UserId, cancellationToken);

        if (totalPoints < item.CostPoints)
        {
            return new RedeemItemResultDto
            {
                Success = false,
                Message = $"Insufficient points. Required: {item.CostPoints}, Available: {totalPoints}.",
                RemainingPoints = totalPoints
            };
        }

        // Deduct points
        var rewardPoint = new RewardPoint
        {
            UserId = request.UserId,
            Points = -item.CostPoints, // Negative points for redemption
            Reason = $"Redeemed: {item.Name}",
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.RewardPoints.AddAsync(rewardPoint);
        await _unitOfWork.SaveChangesAsync();

        // Calculate remaining points
        var remainingPoints = totalPoints - item.CostPoints;

        return new RedeemItemResultDto
        {
            Success = true,
            Message = $"Successfully redeemed '{item.Name}' for {item.CostPoints} points.",
            RemainingPoints = remainingPoints
        };
    }
}


