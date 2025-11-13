using LMS.Application.Features.Analytics.Queries.GetTopEngagedStudents;
using LMS.Application.Features.Analytics.Queries.GetLeaderboard;
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
}

