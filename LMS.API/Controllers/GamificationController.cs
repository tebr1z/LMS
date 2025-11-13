using LMS.Application.Features.Gamification.Commands.RedeemItem;
using LMS.Application.Features.Gamification.Queries.GetGamificationProfile;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LMS.API.Controllers;

[ApiController]
[Route("api/gamification")]
[Authorize]
public class GamificationController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<GamificationController> _logger;

    public GamificationController(IMediator mediator, ILogger<GamificationController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get gamification profile for a user (achievements, points)
    /// </summary>
    [HttpGet("profile/{userId}")]
    [ProducesResponseType(typeof(GamificationProfileDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<GamificationProfileDto>> GetGamificationProfile(
        int userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get current user ID from claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var currentUserId))
            {
                return Unauthorized(new { message = "Invalid user token." });
            }

            // Users can only view their own profile, or admins can view any profile
            var isAdmin = User.IsInRole("Admin") || User.IsInRole("MasterAdmin");
            if (userId != currentUserId && !isAdmin)
            {
                return Forbid("You can only view your own gamification profile.");
            }

            var query = new GetGamificationProfileQuery { UserId = userId };
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving gamification profile for user {UserId}", userId);
            return StatusCode(500, new { message = "An error occurred while retrieving gamification profile.", error = ex.Message });
        }
    }

    /// <summary>
    /// Redeem points for an item
    /// </summary>
    [HttpPost("redeem")]
    [ProducesResponseType(typeof(RedeemItemResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<RedeemItemResultDto>> RedeemItem(
        [FromBody] RedeemItemRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get current user ID from claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid user token." });
            }

            var command = new RedeemItemCommand
            {
                UserId = userId,
                RedeemableItemId = request.RedeemableItemId
            };

            var result = await _mediator.Send(command, cancellationToken);

            if (!result.Success)
            {
                return BadRequest(result);
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error redeeming item for user");
            return StatusCode(500, new { message = "An error occurred while redeeming item.", error = ex.Message });
        }
    }
}

// Request DTO
public class RedeemItemRequest
{
    public int RedeemableItemId { get; set; }
}


