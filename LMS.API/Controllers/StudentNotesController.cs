using LMS.Application.Features.StudentNotes.Commands.AddStudentNote;
using LMS.Application.Features.StudentNotes.Queries.GetStudentNotes;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LMS.API.Controllers;

[ApiController]
[Route("api/student-notes")]
[Authorize]
public class StudentNotesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<StudentNotesController> _logger;

    public StudentNotesController(IMediator mediator, ILogger<StudentNotesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Add a student note (Mentor, Teacher, StudentOffice, Admin only)
    /// Private notes are visible only to StudentOffice & assigned Teachers
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Mentor,Teacher,StudentOffice,Admin,MasterAdmin")]
    public async Task<ActionResult<int>> AddStudentNote(
        [FromBody] AddStudentNoteCommandDto commandDto,
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

            var command = new AddStudentNoteCommand
            {
                StudentId = commandDto.StudentId,
                GroupId = commandDto.GroupId,
                Title = commandDto.Title,
                Content = commandDto.Content,
                IsPrivate = commandDto.IsPrivate,
                IsImportant = commandDto.IsImportant,
                CreatedById = userId
            };

            var result = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetStudentNotes), new { studentId = commandDto.StudentId }, result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when adding student note");
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access when adding student note");
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding student note");
            return StatusCode(500, new { message = "An error occurred while adding student note." });
        }
    }

    /// <summary>
    /// Get student notes (Mentor, Teacher, StudentOffice, Admin can view)
    /// Private notes are only visible to StudentOffice & assigned Teachers
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Mentor,Teacher,StudentOffice,Admin,MasterAdmin")]
    public async Task<ActionResult<IEnumerable<StudentNoteDto>>> GetStudentNotes(
        [FromQuery] int? studentId = null,
        [FromQuery] int? groupId = null,
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

            var query = new GetStudentNotesQuery
            {
                StudentId = studentId,
                GroupId = groupId,
                UserId = userId
            };

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when getting student notes");
            return BadRequest(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access when getting student notes");
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving student notes");
            return StatusCode(500, new { message = "An error occurred while retrieving student notes." });
        }
    }
}

public class AddStudentNoteCommandDto
{
    public int StudentId { get; set; }
    public int GroupId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsPrivate { get; set; } = true;
    public bool IsImportant { get; set; } = false;
}

