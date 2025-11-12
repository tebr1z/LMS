using LMS.Application.Features.Enrollments.Commands.EnrollInCourse;
using LMS.Application.Features.Enrollments.Queries.GetEnrollmentsByUser;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace LMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Student,Instructor,Admin")]
public class EnrollmentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<EnrollmentsController> _logger;
    private readonly IStringLocalizer<EnrollmentsController> _localizer;

    public EnrollmentsController(IMediator mediator, ILogger<EnrollmentsController> logger, IStringLocalizer<EnrollmentsController> localizer)
    {
        _mediator = mediator;
        _logger = logger;
        _localizer = localizer;
    }

    [HttpPost]
    [Authorize(Roles = "Student,Admin")]
    public async Task<ActionResult<int>> EnrollInCourse([FromBody] EnrollInCourseCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(command, cancellationToken);
            return Ok(new { enrollmentId = result, message = _localizer["EnrollmentSuccess"] });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Enrollment failed");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, _localizer["EnrollmentError"]);
            return StatusCode(500, new { message = _localizer["EnrollmentError"] });
        }
    }

    [HttpGet("{userId}")]
    public async Task<ActionResult> GetEnrollmentsByUser(int userId, CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetEnrollmentsByUserQuery { UserId = userId };
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving enrollments");
            return StatusCode(500, new { message = "An error occurred while retrieving enrollments." });
        }
    }
}

