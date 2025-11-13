using LMS.Application.Features.AI.Queries.GetAIFeedback;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.API.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize]
public class AIController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AIController> _logger;

    public AIController(IMediator mediator, ILogger<AIController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get latest AI feedback for a submission
    /// </summary>
    [HttpGet("feedback/{submissionId}")]
    [ProducesResponseType(typeof(AIFeedbackDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AIFeedbackDto>> GetAIFeedback(
        int submissionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetAIFeedbackQuery { SubmissionId = submissionId };
            var result = await _mediator.Send(query, cancellationToken);

            if (result == null)
            {
                return NotFound(new { message = $"AI feedback not found for submission {submissionId}." });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving AI feedback for submission {SubmissionId}", submissionId);
            return StatusCode(500, new { message = "An error occurred while retrieving AI feedback.", error = ex.Message });
        }
    }
}

