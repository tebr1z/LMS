using LMS.Application.Features.Courses.Commands.CreateCourse;
using LMS.Application.Features.Courses.Queries.GetAllCourses;
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
            return CreatedAtAction(nameof(GetAllCourses), new { id = result }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, _localizer["ErrorCreatingCourse"]);
            return BadRequest(new { message = ex.Message });
        }
    }
}

