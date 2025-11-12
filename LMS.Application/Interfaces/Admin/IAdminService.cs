namespace LMS.Application.Interfaces.Admin;

/// <summary>
/// Interface for admin operations
/// </summary>
public interface IAdminService
{
    /// <summary>
    /// Gets system statistics
    /// </summary>
    Task<SystemStatistics> GetSystemStatisticsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets user management data
    /// </summary>
    Task<UserManagementData> GetUserManagementDataAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates user role
    /// </summary>
    Task<bool> UpdateUserRoleAsync(int userId, string role, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets course management data
    /// </summary>
    Task<CourseManagementData> GetCourseManagementDataAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets enrollment statistics
    /// </summary>
    Task<EnrollmentStatistics> GetEnrollmentStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// System statistics model
/// </summary>
public class SystemStatistics
{
    public int TotalUsers { get; set; }
    public int TotalCourses { get; set; }
    public int TotalEnrollments { get; set; }
    public int ActiveUsers { get; set; }
    public Dictionary<string, int> UsersByRole { get; set; } = new();
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// User management data model
/// </summary>
public class UserManagementData
{
    public List<UserInfo> Users { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

/// <summary>
/// User info model
/// </summary>
public class UserInfo
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public int EnrollmentCount { get; set; }
}

/// <summary>
/// Course management data model
/// </summary>
public class CourseManagementData
{
    public List<CourseInfo> Courses { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

/// <summary>
/// Course info model
/// </summary>
public class CourseInfo
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string CreatorName { get; set; } = string.Empty;
    public int EnrollmentCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Enrollment statistics model
/// </summary>
public class EnrollmentStatistics
{
    public int TotalEnrollments { get; set; }
    public int EnrollmentsThisMonth { get; set; }
    public int EnrollmentsThisWeek { get; set; }
    public Dictionary<string, int> EnrollmentsByCourse { get; set; } = new();
    public List<DailyEnrollment> DailyEnrollments { get; set; } = new();
}

/// <summary>
/// Daily enrollment model
/// </summary>
public class DailyEnrollment
{
    public DateTime Date { get; set; }
    public int Count { get; set; }
}

