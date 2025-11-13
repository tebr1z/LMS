using LMS.Application.Features.StudentOffice.Commands.FlagStudent;
using LMS.Application.Features.StudentOffice.Queries.GetAtRiskStudents;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LMS.API.Controllers;

[ApiController]
[Route("api/studentoffice")]
[Authorize]
public class StudentOfficeController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<StudentOfficeController> _logger;

    public StudentOfficeController(IMediator mediator, ILogger<StudentOfficeController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get at-risk students (low scores, missed deadlines, low attendance)
    /// StudentOffice and Admin only
    /// </summary>
    [HttpGet("students/at-risk")]
    [Authorize(Roles = "StudentOffice,Admin,MasterAdmin")]
    public async Task<ActionResult<IEnumerable<AtRiskStudentDto>>> GetAtRiskStudents(
        [FromQuery] decimal? scoreAverageThreshold = null,
        [FromQuery] int? missedDeadlinesThreshold = null,
        [FromQuery] decimal? attendanceThreshold = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetAtRiskStudentsQuery
            {
                ScoreAverageThreshold = scoreAverageThreshold ?? 60,
                MissedDeadlinesThreshold = missedDeadlinesThreshold ?? 3,
                AttendanceThreshold = attendanceThreshold ?? 70
            };

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving at-risk students");
            return StatusCode(500, new { message = "An error occurred while retrieving at-risk students." });
        }
    }

    /// <summary>
    /// Flag a student (StudentOffice and Admin only)
    /// </summary>
    [HttpPost("flag-student")]
    [Authorize(Roles = "StudentOffice,Admin,MasterAdmin")]
    public async Task<ActionResult<int>> FlagStudent(
        [FromBody] FlagStudentCommandDto commandDto,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get current user ID from claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid user token." });
            }

            var command = new FlagStudentCommand
            {
                StudentId = commandDto.StudentId,
                Reason = commandDto.Reason,
                RecommendedAction = commandDto.RecommendedAction,
                CreatedById = userId
            };

            var result = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetAtRiskStudents), new { }, result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when flagging student");
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access when flagging student");
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error flagging student");
            return StatusCode(500, new { message = "An error occurred while flagging student." });
        }
    }

    /// <summary>
    /// Get student progress metrics (time-on-page, submission lateness, quiz pass rates)
    /// StudentOffice and Admin can view
    /// </summary>
    [HttpGet("students/{studentId}/progress")]
    [Authorize(Roles = "StudentOffice,Admin,MasterAdmin")]
    public async Task<ActionResult<StudentProgressMetricsDto>> GetStudentProgress(
        int studentId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Use GetAtRiskStudentsQuery with very lenient thresholds to get progress metrics for any student
            var query = new GetAtRiskStudentsQuery
            {
                ScoreAverageThreshold = 0,
                MissedDeadlinesThreshold = 999,
                AttendanceThreshold = 0
            };

            var atRiskStudents = await _mediator.Send(query, cancellationToken);
            var studentProgress = atRiskStudents.FirstOrDefault(s => s.StudentId == studentId);

            if (studentProgress == null)
            {
                // Student not found in at-risk list, calculate metrics separately
                // For now, return not found - could enhance to calculate for any student
                return NotFound(new { message = $"Student with ID {studentId} not found or has no activity data." });
            }

            return Ok(studentProgress.ProgressMetrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving student progress");
            return StatusCode(500, new { message = "An error occurred while retrieving student progress." });
        }
    }
}

public class FlagStudentCommandDto
{
    public int StudentId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
}

