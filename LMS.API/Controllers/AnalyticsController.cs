using LMS.Application.Features.Analytics.Queries.GetTopEngagedStudents;
using LMS.Application.Features.Analytics.Queries.GetLeaderboard;
using LMS.Application.Features.Analytics.Queries.GetTeacherDashboard;
using LMS.Application.Features.Analytics.Queries.GetEfficiency;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.API.Controllers;

[ApiController]
[Route("api/analytics")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AnalyticsController> _logger;

    public AnalyticsController(IMediator mediator, ILogger<AnalyticsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get top engaged students by time-on-page, quiz attempts, best pass rate
    /// StudentOffice, Teachers, and MasterAdmin can view
    /// </summary>
    [HttpGet("top-engaged-students")]
    [Authorize(Roles = "Teacher,StudentOffice,Admin,MasterAdmin")]
    public async Task<ActionResult<IEnumerable<EngagedStudentDto>>> GetTopEngagedStudents(
        [FromQuery] int? courseId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int? topN = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetTopEngagedStudentsQuery
            {
                CourseId = courseId,
                FromDate = from,
                ToDate = to,
                TopN = topN ?? 10
            };

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving top engaged students");
            return StatusCode(500, new { message = "An error occurred while retrieving analytics." });
        }
    }

    /// <summary>
    /// Get leaderboard of top students ordered by averagePercent, submissions, lastActivity
    /// Teachers and StudentOffice can view
    /// </summary>
    [HttpGet("leaderboard")]
    [Authorize(Roles = "Teacher,StudentOffice,Admin,MasterAdmin")]
    public async Task<ActionResult<IEnumerable<LeaderboardEntryDto>>> GetLeaderboard(
        [FromQuery] int? courseInstanceId = null,
        [FromQuery] string period = "overall",
        [FromQuery] int? topN = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate period
            var validPeriods = new[] { "week", "month", "overall" };
            if (!validPeriods.Contains(period?.ToLower()))
            {
                return BadRequest(new { message = $"Period must be one of: {string.Join(", ", validPeriods)}" });
            }

            var query = new GetLeaderboardQuery
            {
                CourseInstanceId = courseInstanceId,
                Period = period?.ToLower() ?? "overall",
                TopN = topN ?? 10
            };

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving leaderboard");
            return StatusCode(500, new { message = "An error occurred while retrieving leaderboard." });
        }
    }

    /// <summary>
    /// Get teacher dashboard with summarized data including average score, late submissions, active groups, top students, and time distribution
    /// Teachers can view their own dashboard, Admin and MasterAdmin can view any teacher's dashboard
    /// </summary>
    [HttpGet("teacher-dashboard")]
    [Authorize(Roles = "Teacher,Admin,MasterAdmin")]
    [ProducesResponseType(typeof(TeacherDashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TeacherDashboardDto>> GetTeacherDashboard(
        [FromQuery] int teacherId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Get current user ID and role from claims
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userRoleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ??
                               User.FindFirst("Role")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var currentUserId))
            {
                return Unauthorized(new { message = "Invalid user token." });
            }

            // Authorization: Teacher can only view their own dashboard
            if (userRoleClaim == "Teacher" && currentUserId != teacherId)
            {
                return Forbid("Teachers can only view their own dashboard.");
            }

            var query = new GetTeacherDashboardQuery
            {
                TeacherId = teacherId
            };

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving teacher dashboard for teacher {TeacherId}", teacherId);
            return StatusCode(500, new { message = "An error occurred while retrieving teacher dashboard.", error = ex.Message });
        }
    }

    /// <summary>
    /// Get student efficiency analysis based on average scores and time spent on assignments
    /// Calculates EfficiencyScore = AverageScore / AvgTimeOnPageInMinutes
    /// Accessible by Teacher, StudentOffice, Admin, and MasterAdmin
    /// </summary>
    [HttpGet("efficiency")]
    [Authorize(Roles = "Teacher,StudentOffice,Admin,MasterAdmin")]
    [ProducesResponseType(typeof(List<EfficiencyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<List<EfficiencyDto>>> GetEfficiency(
        [FromQuery] int? courseInstanceId = null,
        [FromQuery] int? groupId = null,
        [FromQuery] int? topN = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetEfficiencyQuery
            {
                CourseInstanceId = courseInstanceId,
                GroupId = groupId,
                TopN = topN
            };

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving efficiency analytics");
            return StatusCode(500, new { message = "An error occurred while retrieving efficiency analytics.", error = ex.Message });
        }
    }
}

