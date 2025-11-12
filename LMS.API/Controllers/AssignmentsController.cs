using LMS.Application.Features.Assignments.Commands.CreateAssignment;
using LMS.Application.Features.Assignments.Commands.GradeAssignment;
using LMS.Application.Features.Assignments.Commands.SubmitAssignment;
using LMS.Application.Features.Assignments.Queries.GetAssignmentsByCourse;
using LMS.Application.Features.Assignments.Queries.GetAssignmentSubmissions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AssignmentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AssignmentsController> _logger;

    public AssignmentsController(IMediator mediator, ILogger<AssignmentsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Create a new assignment (Only Teacher, Admin, MasterAdmin)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Teacher,Admin,MasterAdmin")]
    public async Task<ActionResult<int>> CreateAssignment([FromBody] CreateAssignmentCommand command, CancellationToken cancellationToken)
    {
        try
        {
            // Get current user ID from claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid user token." });
            }

            command.CreatedBy = userId;
            var result = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetAssignmentsByCourse), new { courseId = command.CourseId }, result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when creating assignment");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating assignment");
            return StatusCode(500, new { message = "An error occurred while creating assignment." });
        }
    }

    /// <summary>
    /// Get all assignments by course ID (Teacher, Student, Mentor, Admin, MasterAdmin can view)
    /// </summary>
    [HttpGet("by-course/{courseId}")]
    [Authorize(Roles = "Teacher,Student,Mentor,Admin,MasterAdmin")]
    public async Task<ActionResult> GetAssignmentsByCourse(int courseId, CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetAssignmentsByCourseQuery { CourseId = courseId };
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Course not found");
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving assignments");
            return StatusCode(500, new { message = "An error occurred while retrieving assignments." });
        }
    }

    /// <summary>
    /// Submit an assignment (Only Student, deadline must not be passed)
    /// </summary>
    [HttpPost("{assignmentId}/submit")]
    [Authorize(Roles = "Student")]
    public async Task<ActionResult<int>> SubmitAssignment(
        int assignmentId,
        [FromBody] SubmitAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get current user ID from claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var studentId))
            {
                return Unauthorized(new { message = "Invalid user token." });
            }

            var command = new SubmitAssignmentCommand
            {
                AssignmentId = assignmentId,
                StudentId = studentId,
                FileUrl = request.FileUrl,
                AnswerText = request.AnswerText
            };

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(new { submissionId = result, message = "Assignment submitted successfully." });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized attempt to submit assignment");
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when submitting assignment");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting assignment");
            return StatusCode(500, new { message = "An error occurred while submitting assignment." });
        }
    }

    /// <summary>
    /// Get all submissions for a specific assignment (Only Teacher, Admin, MasterAdmin)
    /// </summary>
    [HttpGet("{id}/submissions")]
    [Authorize(Roles = "Teacher,Admin,MasterAdmin")]
    public async Task<ActionResult> GetAssignmentSubmissions(int id, CancellationToken cancellationToken)
    {
        try
        {
            var query = new GetAssignmentSubmissionsQuery { AssignmentId = id };
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Assignment not found");
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving assignment submissions");
            return StatusCode(500, new { message = "An error occurred while retrieving assignment submissions." });
        }
    }

    /// <summary>
    /// Grade an assignment (Only Teacher, Admin, MasterAdmin)
    /// </summary>
    [HttpPost("{assignmentId}/score")]
    [Authorize(Roles = "Teacher,Admin,MasterAdmin")]
    public async Task<ActionResult> GradeAssignment(
        int assignmentId,
        [FromBody] GradeAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get current user ID from claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var evaluatorId))
            {
                return Unauthorized(new { message = "Invalid user token." });
            }

            var command = new GradeAssignmentCommand
            {
                AssignmentId = assignmentId,
                SubmissionId = request.SubmissionId,
                Score = request.Score,
                EvaluatedBy = evaluatorId
            };

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(new { success = result, message = "Assignment graded successfully." });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized attempt to grade assignment");
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when grading assignment");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error grading assignment");
            return StatusCode(500, new { message = "An error occurred while grading assignment." });
        }
    }
}

// Request DTOs
public class SubmitAssignmentRequest
{
    public string? FileUrl { get; set; }
    public string? AnswerText { get; set; }
}

public class GradeAssignmentRequest
{
    public int SubmissionId { get; set; }
    public decimal Score { get; set; }
}

