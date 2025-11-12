using LMS.Application.Interfaces.Admin;
using LMS.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LMS.API.Controllers.Admin;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(IAdminService adminService, ILogger<AdminController> logger)
    {
        _adminService = adminService;
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
}

public class UpdateRoleRequest
{
    public string Role { get; set; } = string.Empty;
}

