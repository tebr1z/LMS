using LMS.Application.Features.Admin.Queries.GetAuditLogs;
using LMS.Application.Features.Admin.Queries.GetDashboard;
using LMS.Application.Interfaces.Admin;
using LMS.API.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.API.Controllers.Admin;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "MasterAdmin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly IMediator _mediator;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IAdminService adminService,
        IMediator mediator,
        ILogger<AdminController> logger)
    {
        _adminService = adminService;
        _mediator = mediator;
        _logger = logger;
    }

    [HttpGet("statistics")]
    public async Task<ActionResult<ApiResponse<SystemStatistics>>> GetSystemStatistics(CancellationToken cancellationToken)
    {
        try
        {
            var statistics = await _adminService.GetSystemStatisticsAsync(cancellationToken);
            return Ok(ApiResponse<SystemStatistics>.SuccessResponse(statistics, "System statistics retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving system statistics");
            return StatusCode(500, ApiResponse<SystemStatistics>.ErrorResponse("An error occurred while retrieving statistics", ex.Message));
        }
    }

    [HttpGet("users")]
    public async Task<ActionResult<ApiResponse<UserManagementData>>> GetUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var data = await _adminService.GetUserManagementDataAsync(pageNumber, pageSize, cancellationToken);
            return Ok(ApiResponse<UserManagementData>.SuccessResponse(data, "Users retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving users");
            return StatusCode(500, ApiResponse<UserManagementData>.ErrorResponse("An error occurred while retrieving users", ex.Message));
        }
    }

    [HttpPut("users/{userId}/role")]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateUserRole(
        int userId,
        [FromBody] UpdateRoleRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _adminService.UpdateUserRoleAsync(userId, request.Role, cancellationToken);
            return Ok(ApiResponse<bool>.SuccessResponse(result, "User role updated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user role");
            return StatusCode(500, ApiResponse<bool>.ErrorResponse("An error occurred while updating user role", ex.Message));
        }
    }

    [HttpGet("courses")]
    public async Task<ActionResult<ApiResponse<CourseManagementData>>> GetCourses(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var data = await _adminService.GetCourseManagementDataAsync(pageNumber, pageSize, cancellationToken);
            return Ok(ApiResponse<CourseManagementData>.SuccessResponse(data, "Courses retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving courses");
            return StatusCode(500, ApiResponse<CourseManagementData>.ErrorResponse("An error occurred while retrieving courses", ex.Message));
        }
    }

    [HttpGet("enrollments/statistics")]
    public async Task<ActionResult<ApiResponse<EnrollmentStatistics>>> GetEnrollmentStatistics(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var statistics = await _adminService.GetEnrollmentStatisticsAsync(startDate, endDate, cancellationToken);
            return Ok(ApiResponse<EnrollmentStatistics>.SuccessResponse(statistics, "Enrollment statistics retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving enrollment statistics");
            return StatusCode(500, ApiResponse<EnrollmentStatistics>.ErrorResponse("An error occurred while retrieving enrollment statistics", ex.Message));
        }
    }

    /// <summary>
    /// Get all groups (MasterAdmin only)
    /// </summary>
    [HttpGet("groups")]
    public async Task<ActionResult<ApiResponse<GroupManagementData>>> GetGroups(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var data = await _adminService.GetGroupManagementDataAsync(pageNumber, pageSize, cancellationToken);
            return Ok(ApiResponse<GroupManagementData>.SuccessResponse(data, "Groups retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving groups");
            return StatusCode(500, ApiResponse<GroupManagementData>.ErrorResponse("An error occurred while retrieving groups", ex.Message));
        }
    }

    /// <summary>
    /// Get admin dashboard with system-wide stats (MasterAdmin only)
    /// </summary>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(DashboardDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DashboardDto>> GetDashboard(CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetDashboardQuery();
            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving dashboard");
            return StatusCode(500, new { message = "An error occurred while retrieving dashboard.", error = ex.Message });
        }
    }

    /// <summary>
    /// Get audit logs (MasterAdmin only)
    /// </summary>
    [HttpGet("audit-logs")]
    [ProducesResponseType(typeof(AuditLogsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AuditLogsDto>> GetAuditLogs(
        [FromQuery] string? entity = null,
        [FromQuery] int? entityId = null,
        [FromQuery] string? action = null,
        [FromQuery] int? userId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetAuditLogsQuery
            {
                Entity = entity,
                EntityId = entityId,
                Action = action,
                UserId = userId,
                StartDate = startDate,
                EndDate = endDate,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var result = await _mediator.Send(query, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit logs");
            return StatusCode(500, new { message = "An error occurred while retrieving audit logs.", error = ex.Message });
        }
    }
}

public class UpdateRoleRequest
{
    public string Role { get; set; } = string.Empty;
}

