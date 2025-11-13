using LMS.Application.Features.Analytics.Queries.GetTopEngagedStudents;
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
}

