using LMS.Application.Features.Courses.Commands.CreateCourse;
using LMS.Application.Features.Courses.Queries.GetAllCourses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace LMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Student,Teacher,Admin,MasterAdmin")]
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
    [Authorize(Roles = "Student,Teacher,Admin,MasterAdmin")]
    public async Task<ActionResult> GetAllCourses(CancellationToken cancellationToken)
    {
        var query = new GetAllCoursesQuery();
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

