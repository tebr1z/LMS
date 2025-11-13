using LMS.Application.Features.Learning.Queries.GetLearningLevel;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.API.Controllers;

[ApiController]
[Route("api/learning-level")]
[Authorize]
public class LearningController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<LearningController> _logger;

    public LearningController(IMediator mediator, ILogger<LearningController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get learning level (difficulty level) for a student
    /// </summary>
    [HttpGet("{studentId}")]
    [ProducesResponseType(typeof(LearningLevelDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<LearningLevelDto>> GetLearningLevel(int studentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetLearningLevelQuery { StudentId = studentId };
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving learning level for student {StudentId}", studentId);
            return StatusCode(500, new { message = "An error occurred while retrieving learning level.", error = ex.Message });
        }
    }
}

