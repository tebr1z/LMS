using LMS.Application.Features.Telemetry.Commands.RecordAssignmentTime;
using LMS.Application.Features.Telemetry.Commands.RecordQuizTime;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.API.Controllers;

[ApiController]
[Route("api/telemetry")]
[Authorize] // Require authentication
public class TelemetryController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<TelemetryController> _logger;

    public TelemetryController(IMediator mediator, ILogger<TelemetryController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Record assignment time telemetry (frontend posts visibility events)
    /// Aggregate per submission and store in AssignmentSubmission.TimeOnPageInSeconds
    /// </summary>
    [HttpPost("assignment-time")]
    [Authorize(Roles = "Student")] // Only students can post their own telemetry
    public async Task<ActionResult> RecordAssignmentTime(
        [FromBody] RecordAssignmentTimeCommandDto commandDto,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get current user ID from claims
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid user token." });
            }

            // Ensure student can only record their own telemetry
            if (commandDto.StudentId != userId)
            {
                return Forbid();
            }

            var command = new RecordAssignmentTimeCommand
            {
                StudentId = commandDto.StudentId,
                AssignmentId = commandDto.AssignmentId,
                SecondsActive = commandDto.SecondsActive,
                SessionId = commandDto.SessionId,
                Timestamp = commandDto.Timestamp ?? DateTime.UtcNow
            };

            await _mediator.Send(command, cancellationToken);
            return Ok(new { Message = "Assignment telemetry recorded successfully." });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when recording assignment telemetry");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording assignment telemetry");
            return StatusCode(500, new { message = "An error occurred while recording telemetry." });
        }
    }

    /// <summary>
    /// Record quiz time telemetry (frontend posts visibility events)
    /// </summary>
    [HttpPost("quiz-time")]
    [Authorize(Roles = "Student")] // Only students can post their own telemetry
    public async Task<ActionResult> RecordQuizTime(
        [FromBody] RecordQuizTimeCommandDto commandDto,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get current user ID from claims
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid user token." });
            }

            // Ensure student can only record their own telemetry
            if (commandDto.StudentId != userId)
            {
                return Forbid();
            }

            var command = new RecordQuizTimeCommand
            {
                StudentId = commandDto.StudentId,
                QuizId = commandDto.QuizId,
                SecondsActive = commandDto.SecondsActive,
                SessionId = commandDto.SessionId,
                Timestamp = commandDto.Timestamp ?? DateTime.UtcNow,
                QuizSessionId = commandDto.QuizSessionId
            };

            await _mediator.Send(command, cancellationToken);
            return Ok(new { Message = "Quiz telemetry recorded successfully." });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when recording quiz telemetry");
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access when recording quiz telemetry");
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording quiz telemetry");
            return StatusCode(500, new { message = "An error occurred while recording telemetry." });
        }
    }
}

public class RecordAssignmentTimeCommandDto
{
    public int StudentId { get; set; }
    public int AssignmentId { get; set; }
    public int SecondsActive { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public DateTime? Timestamp { get; set; }
}

public class RecordQuizTimeCommandDto
{
    public int StudentId { get; set; }
    public int QuizId { get; set; }
    public int SecondsActive { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public DateTime? Timestamp { get; set; }
    public int? QuizSessionId { get; set; }
}

