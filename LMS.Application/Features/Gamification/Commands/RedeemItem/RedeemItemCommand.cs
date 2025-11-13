using MediatR;

namespace LMS.Application.Features.Gamification.Commands.RedeemItem;

public class RedeemItemCommand : IRequest<RedeemItemResultDto>
{
    public int UserId { get; set; }
    public int RedeemableItemId { get; set; }
}

public class RedeemItemResultDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int RemainingPoints { get; set; }
}

