using LMS.Application.Features.Courses.Commands.CreateCourse;
using LMS.Application.Features.Courses.Commands.TranslateCourse;
using LMS.Application.Features.Courses.Queries.GetAllCourses;
using LMS.Application.Features.Courses.Queries.GetCourseById;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Security.Claims;

namespace LMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Student,Teacher,Admin,MasterAdmin,Mentor")]
public class CoursesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<CoursesController> _logger;
    private readonly IStringLocalizer<CoursesController> _localizer;

    public CoursesController(IMediator mediator, ILogger<CoursesController> logger, IStringLocalizer<CoursesController> localizer)
    {
        _mediator = mediator;
        _logger = logger;
        _localizer = localizer;
    }

    [HttpGet]
    [Authorize(Roles = "Student,Teacher,Admin,MasterAdmin,Mentor")]
    public async Task<ActionResult> GetAllCourses(CancellationToken cancellationToken)
    {
        // Get current user ID and role from claims
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userRoleClaim = User.FindFirst(ClaimTypes.Role)?.Value ?? 
                           User.FindFirst("Role")?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new { message = "Invalid user token." });
        }

        var query = new GetAllCoursesQuery
        {
            UserId = userId,
            UserRole = userRoleClaim ?? string.Empty
        };
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }

    [HttpPost]
    [Authorize(Roles = "Teacher,Admin,MasterAdmin")]
    public async Task<ActionResult<int>> CreateCourse([FromBody] CreateCourseCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetCourseById), new { id = result }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, _localizer["ErrorCreatingCourse"]);
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get course by ID with optional language localization
    /// </summary>
    /// <param name="courseId">Course ID</param>
    /// <param name="lang">Optional language code (e.g., "tr", "ru", "az", "en"). If not provided, returns original/default version.</param>
    [HttpGet("{courseId}")]
    [Authorize(Roles = "Student,Teacher,Admin,MasterAdmin,Mentor")]
    [ProducesResponseType(typeof(CourseLocalizedDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<CourseLocalizedDto>> GetCourseById(
        int courseId,
        [FromQuery] string? lang = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetCourseByIdQuery
            {
                CourseId = courseId,
                LangCode = lang
            };

            var result = await _mediator.Send(query, cancellationToken);

            if (result == null)
            {
                return NotFound(new { message = $"Course with ID {courseId} not found." });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving course {CourseId}", courseId);
            return StatusCode(500, new { message = "An error occurred while retrieving the course.", error = ex.Message });
        }
    }

    /// <summary>
    /// Translate a course to a specific language
    /// </summary>
    /// <param name="courseId">Course ID</param>
    /// <param name="lang">Target language code (e.g., "tr", "ru", "az")</param>
    [HttpPost("translate/{courseId}")]
    [Authorize(Roles = "Teacher,Admin,MasterAdmin")]
    [ProducesResponseType(typeof(TranslateCourseResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TranslateCourseResponse>> TranslateCourse(
        int courseId,
        [FromQuery] string lang,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(lang))
            {
                return BadRequest(new { message = "Language code (lang) parameter is required." });
            }

            var command = new TranslateCourseCommand
            {
                CourseId = courseId,
                LangCode = lang
            };

            var result = await _mediator.Send(command, cancellationToken);

            if (!result.Success)
            {
                if (result.ErrorMessage?.Contains("not found") == true)
                {
                    return NotFound(new { message = result.ErrorMessage });
                }
                return BadRequest(new { message = result.Message, error = result.ErrorMessage });
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error translating course {CourseId} to language {Lang}", courseId, lang);
            return StatusCode(500, new { message = "An error occurred during translation.", error = ex.Message });
        }
    }
}

