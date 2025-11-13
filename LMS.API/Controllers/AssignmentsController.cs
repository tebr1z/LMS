using LMS.Application.Features.Assignments.Commands.CreateAssignment;
using LMS.Application.Features.Assignments.Commands.GradeAssignment;
using LMS.Application.Features.Assignments.Commands.SubmitAssignment;
using LMS.Application.Features.Assignments.Queries.GetAssignmentsByCourse;
using LMS.Application.Features.Assignments.Queries.GetAssignmentSubmissions;
using LMS.Application.Features.Learning.Queries.GetDifficultySuggestions;
using LMS.Application.Interfaces.Storage;
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
    private readonly IFileStorageService _fileStorageService;

    public AssignmentsController(
        IMediator mediator, 
        ILogger<AssignmentsController> logger,
        IFileStorageService fileStorageService)
    {
        _mediator = mediator;
        _logger = logger;
        _fileStorageService = fileStorageService;
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

            command.CreatedById = userId;
            var result = await _mediator.Send(command, cancellationToken);
            return CreatedAtAction(nameof(GetAssignmentsByCourse), new { courseId = command.CourseInstanceId }, result);
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
    /// Supports file upload (PDF, video, image) and/or text answer
    /// </summary>
    [HttpPost("{assignmentId}/submit")]
    [Authorize(Roles = "Student")]
    [RequestSizeLimit(100_000_000)] // 100 MB limit
    public async Task<ActionResult<int>> SubmitAssignment(
        int assignmentId,
        [FromForm] SubmitAssignmentRequest request,
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

            string? fileUrl = null;

            // Handle file upload if provided
            if (request.File != null && request.File.Length > 0)
            {
                // Validate file type - Only allow: pdf, jpg, png, mp4, docx
                var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png", ".mp4", ".docx" };
                var fileExtension = Path.GetExtension(request.File.FileName).ToLowerInvariant();
                
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return BadRequest(new { message = "Invalid file type. Allowed types: PDF, JPG, PNG, MP4, DOCX." });
                }

                // Validate file size (100 MB max)
                if (request.File.Length > 100_000_000)
                {
                    return BadRequest(new { message = "File size exceeds maximum limit of 100 MB." });
                }

                // Upload file to wwwroot/uploads/assignments
                using var fileStream = request.File.OpenReadStream();
                fileUrl = await _fileStorageService.UploadFileAsync(
                    fileStream,
                    request.File.FileName,
                    request.File.ContentType,
                    folder: "assignments",
                    cancellationToken);
            }

            var command = new SubmitAssignmentCommand
            {
                AssignmentId = assignmentId,
                StudentId = studentId,
                FileUrl = fileUrl ?? request.FileUrl,
                AnswerText = request.AnswerText
            };

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(new { submissionId = result, message = "Assignment submitted successfully.", fileUrl });
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
    /// Get all submissions for a specific assignment (Teacher, Admin, MasterAdmin, Mentor - read-only)
    /// </summary>
    [HttpGet("{id}/submissions")]
    [Authorize(Roles = "Teacher,Admin,MasterAdmin,Mentor")]
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
                SubmissionId = request.SubmissionId,
                Score = (int)request.Score, // Convert decimal to int
                EvaluatedById = evaluatorId,
                Feedback = request.Feedback ?? null
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
    /// <summary>
    /// File to upload (PDF, image, or video). Can be null if only text answer is provided.
    /// </summary>
    public IFormFile? File { get; set; }
    
    /// <summary>
    /// File URL (for backward compatibility or when file is uploaded separately)
    /// </summary>
    public string? FileUrl { get; set; }
    
    /// <summary>
    /// Text answer (optional, required if no file is provided)
    /// </summary>
    public string? AnswerText { get; set; }
}

public class GradeAssignmentRequest
{
    public int SubmissionId { get; set; }
    public decimal Score { get; set; }
    public string? Feedback { get; set; }
}

