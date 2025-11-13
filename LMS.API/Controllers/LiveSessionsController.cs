using LMS.Application.Features.LiveSessions.Commands.EndLiveSession;
using LMS.Application.Features.LiveSessions.Commands.StartLiveSession;
using LMS.Application.Features.LiveSessions.Queries.GetCurrentLiveSession;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LMS.API.Controllers;

[ApiController]
[Route("api/live")]
[Authorize]
public class LiveSessionsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<LiveSessionsController> _logger;

    public LiveSessionsController(IMediator mediator, ILogger<LiveSessionsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Start a live session for a group (Teacher only)
    /// </summary>
    [HttpPost("start/{groupId}")]
    [Authorize(Roles = "Teacher,Admin,MasterAdmin")]
    [ProducesResponseType(typeof(StartLiveSessionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<StartLiveSessionResultDto>> StartSession(
        int groupId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid user token." });
            }

            var command = new StartLiveSessionCommand
            {
                GroupId = groupId,
                TeacherId = userId
            };

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when starting live session for group {GroupId}", groupId);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting live session for group {GroupId}", groupId);
            return StatusCode(500, new { message = "An error occurred while starting live session.", error = ex.Message });
        }
    }

    /// <summary>
    /// End a live session (Teacher only, must be session creator)
    /// </summary>
    [HttpPut("end/{sessionId}")]
    [Authorize(Roles = "Teacher,Admin,MasterAdmin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> EndSession(
        int sessionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid user token." });
            }

            var command = new EndLiveSessionCommand
            {
                SessionId = sessionId,
                TeacherId = userId
            };

            await _mediator.Send(command, cancellationToken);
            return Ok(new { message = "Session ended successfully." });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when ending live session {SessionId}", sessionId);
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized attempt to end live session {SessionId}", sessionId);
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending live session {SessionId}", sessionId);
            return StatusCode(500, new { message = "An error occurred while ending live session.", error = ex.Message });
        }
    }

    /// <summary>
    /// Get current active live session for a group (Student, Teacher, Admin)
    /// Returns JWT-signed URL for secure access
    /// </summary>
    [HttpGet("current/{groupId}")]
    [Authorize(Roles = "Student,Teacher,Admin,MasterAdmin")]
    [ProducesResponseType(typeof(CurrentLiveSessionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CurrentLiveSessionDto>> GetCurrentSession(
        int groupId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid user token." });
            }

            var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "Student";

            var query = new GetCurrentLiveSessionQuery
            {
                GroupId = groupId,
                UserId = userId,
                UserRole = userRole
            };

            var result = await _mediator.Send(query, cancellationToken);

            if (result == null)
            {
                return NotFound(new { message = $"No active live session found for group {groupId}." });
            }

            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to live session for group {GroupId}", groupId);
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current live session for group {GroupId}", groupId);
            return StatusCode(500, new { message = "An error occurred while retrieving live session.", error = ex.Message });
        }
    }
}

