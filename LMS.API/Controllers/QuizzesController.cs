using LMS.Application.Features.Quizzes.Commands.CreateQuiz;
using LMS.Application.Features.Quizzes.Queries.GetQuizStatisticsByGroup;
using LMS.Application.Features.Quizzes.Queries.GetQuizStatisticsByStudent;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class QuizzesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<QuizzesController> _logger;

    public QuizzesController(IMediator mediator, ILogger<QuizzesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Create a new quiz for an assignment (Only Teacher, Admin, MasterAdmin)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Teacher,Admin,MasterAdmin")]
    public async Task<ActionResult<int>> CreateQuiz([FromBody] CreateQuizCommand command, CancellationToken cancellationToken)
    {
        try
        {
            // Get current user ID from claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid user token." });
            }

            command.CreatedById = userId;
            var result = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetQuizStatisticsByStudent), new { studentId = 0 }, result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when creating quiz");
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access when creating quiz");
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating quiz");
            return StatusCode(500, new { message = "An error occurred while creating quiz." });
        }
    }

    /// <summary>
    /// Get quiz statistics for a specific student (Teacher, Admin, or the student themselves)
    /// </summary>
    [HttpGet("statistics/student/{studentId}")]
    [Authorize(Roles = "Teacher,Student,Admin,MasterAdmin")]
    public async Task<ActionResult<QuizStatisticsDto>> GetQuizStatisticsByStudent(
        int studentId,
        [FromQuery] int? quizId = null,
        [FromQuery] int? assignmentId = null,
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

            // Students can only view their own statistics
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
            if (roleClaim == "Student" && userId != studentId)
            {
                return Forbid();
            }

            var query = new GetQuizStatisticsByStudentQuery
            {
                StudentId = studentId,
                QuizId = quizId,
                AssignmentId = assignmentId
            };

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when getting quiz statistics");
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving quiz statistics");
            return StatusCode(500, new { message = "An error occurred while retrieving quiz statistics." });
        }
    }

    /// <summary>
    /// Get quiz statistics for a group (Teacher, Admin, MasterAdmin only)
    /// </summary>
    [HttpGet("statistics/group/{groupId}")]
    [Authorize(Roles = "Teacher,Admin,MasterAdmin")]
    public async Task<ActionResult<GroupQuizStatisticsDto>> GetQuizStatisticsByGroup(
        int groupId,
        [FromQuery] int? quizId = null,
        [FromQuery] int? assignmentId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetQuizStatisticsByGroupQuery
            {
                GroupId = groupId,
                QuizId = quizId,
                AssignmentId = assignmentId
            };

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when getting group quiz statistics");
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving group quiz statistics");
            return StatusCode(500, new { message = "An error occurred while retrieving group quiz statistics." });
        }
    }
}

