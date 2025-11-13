using LMS.Application.Features.Attendance.Commands.AddAttendance;
using LMS.Application.Features.Attendance.Queries.GetAttendance;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LMS.API.Controllers;

[ApiController]
[Route("api/groups/{groupId}/attendance")]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AttendanceController> _logger;

    public AttendanceController(IMediator mediator, ILogger<AttendanceController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Add attendance record (Mentor, Teacher, StudentOffice, Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Mentor,Teacher,StudentOffice,Admin,MasterAdmin")]
    public async Task<ActionResult<int>> AddAttendance(
        int groupId,
        [FromBody] AddAttendanceCommandDto commandDto,
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

            var command = new AddAttendanceCommand
            {
                GroupId = groupId,
                StudentId = commandDto.StudentId,
                Date = commandDto.Date,
                Present = commandDto.Present,
                Notes = commandDto.Notes,
                MarkedById = userId
            };

            var result = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetAttendance), new { groupId = groupId }, result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when adding attendance");
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access when adding attendance");
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding attendance");
            return StatusCode(500, new { message = "An error occurred while adding attendance." });
        }
    }

    /// <summary>
    /// Get attendance records for a group (Mentor, Teacher, StudentOffice, Admin can view)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Mentor,Teacher,StudentOffice,Admin,MasterAdmin")]
    public async Task<ActionResult<IEnumerable<AttendanceDto>>> GetAttendance(
        int groupId,
        [FromQuery] DateTime? date = null,
        [FromQuery] int? studentId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetAttendanceQuery
            {
                GroupId = groupId,
                Date = date,
                StudentId = studentId
            };

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when getting attendance");
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving attendance");
            return StatusCode(500, new { message = "An error occurred while retrieving attendance." });
        }
    }
}

public class AddAttendanceCommandDto
{
    public int StudentId { get; set; }
    public DateTime Date { get; set; }
    public bool Present { get; set; }
    public string? Notes { get; set; }
}

