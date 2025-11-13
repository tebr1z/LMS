using LMS.Application.Features.Groups.Commands.AddCourseToGroup;
using LMS.Application.Features.Groups.Commands.AddUserToGroup;
using LMS.Application.Features.Groups.Commands.CreateGroup;
using LMS.Application.Features.Groups.Queries.GetGroupDetails;
using LMS.Application.Features.Groups.Queries.GetGroupsByCourse;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace LMS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GroupsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<GroupsController> _logger;

    public GroupsController(IMediator mediator, ILogger<GroupsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Create a new group (Only MasterAdmin or Admin)
    /// </summary>
    [HttpPost]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<int>> CreateGroup([FromBody] CreateGroupCommand command, CancellationToken cancellationToken)
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
            return CreatedAtAction(nameof(GetGroupDetails), new { id = result }, result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized attempt to create group");
            return Forbid(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating group");
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Add a user to a group (MasterAdmin, Admin, or assigned Teacher)
    /// </summary>
    [HttpPost("{groupId}/add-user")]
    [Authorize(Policy = "Teacher")]
    public async Task<ActionResult<int>> AddUserToGroup(
        int groupId,
        [FromBody] AddUserToGroupRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get current user ID from claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var addedBy))
            {
                return Unauthorized(new { message = "Invalid user token." });
            }

            // Parse GroupRole from string
            if (!System.Enum.TryParse<LMS.Domain.Enums.GroupRole>(request.Role ?? "Student", out var groupRole))
            {
                return BadRequest(new { message = "Invalid role. Valid roles: Teacher, Mentor, Student, StudentOffice, Finance" });
            }

            var command = new AddUserToGroupCommand
            {
                GroupId = groupId,
                UserId = request.UserId,
                Role = groupRole,
                AddedBy = addedBy
            };

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(new { groupUserId = result, message = "User added to group successfully." });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized attempt to add user to group");
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when adding user to group");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding user to group");
            return StatusCode(500, new { message = "An error occurred while adding user to group." });
        }
    }

    /// <summary>
    /// Add a course template (CoursePrepared) to a group (Only MasterAdmin or Admin)
    /// When added, creates a Course instance for that Group with copy of DefaultContent
    /// </summary>
    [HttpPost("{groupId}/add-course")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<int>> AddCourseToGroup(
        int groupId,
        [FromBody] AddCourseToGroupRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get current user ID from claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var addedBy))
            {
                return Unauthorized(new { message = "Invalid user token." });
            }

            var command = new AddCourseToGroupCommand
            {
                GroupId = groupId,
                CoursePreparedId = request.CoursePreparedId,
                AddedBy = addedBy,
                CopyAssignmentsFlag = request.CopyAssignmentsFlag
            };

            var result = await _mediator.Send(command, cancellationToken);
            return Ok(new { courseGroupId = result, message = "Course template added to group successfully. Course instance created." });
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized attempt to add course to group");
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation when adding course to group");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding course to group");
            return StatusCode(500, new { message = "An error occurred while adding course to group." });
        }
    }

    /// <summary>
    /// Get group details by ID (returns group details, courses in group, users with roles)
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Policy = "Student")]
    public async Task<ActionResult> GetGroupDetails(int id, CancellationToken cancellationToken)
    {
        try
        {
            // Get current user ID and role from claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRoleClaim = User.FindFirst(ClaimTypes.Role)?.Value ?? 
                               User.FindFirst("Role")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid user token." });
            }

            var query = new GetGroupDetailsQuery 
            { 
                GroupId = id,
                UserId = userId,
                UserRole = userRoleClaim ?? string.Empty
            };
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to group");
            return Forbid(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Group not found");
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving group details");
            return StatusCode(500, new { message = "An error occurred while retrieving group details." });
        }
    }

    /// <summary>
    /// Get all groups by CoursePrepared ID
    /// </summary>
    [HttpGet("by-course/{coursePreparedId}")]
    [Authorize(Policy = "Student")]
    public async Task<ActionResult> GetGroupsByCourse(int coursePreparedId, CancellationToken cancellationToken)
    {
        try
        {
            // Get current user ID and role from claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRoleClaim = User.FindFirst(ClaimTypes.Role)?.Value ?? 
                               User.FindFirst("Role")?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Invalid user token." });
            }

            var query = new GetGroupsByCourseQuery 
            { 
                CoursePreparedId = coursePreparedId,
                UserId = userId,
                UserRole = userRoleClaim ?? string.Empty
            };
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "CoursePrepared not found");
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving groups by course");
            return StatusCode(500, new { message = "An error occurred while retrieving groups." });
        }
    }
}

// Request DTOs
public class AddUserToGroupRequest
{
    public int UserId { get; set; }
    public string? Role { get; set; } // GroupRole enum: Teacher, Mentor, Student, StudentOffice, Finance
}

public class AddCourseToGroupRequest
{
    public int CoursePreparedId { get; set; }
    public bool CopyAssignmentsFlag { get; set; } = false; // If true, copy template assignments metadata
}

