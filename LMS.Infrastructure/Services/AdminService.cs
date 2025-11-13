using LMS.Application.Interfaces;
using LMS.Application.Interfaces.Admin;
using Microsoft.Extensions.Logging;

namespace LMS.Infrastructure.Services;

/// <summary>
/// Admin service implementation
/// </summary>
public class AdminService : IAdminService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<AdminService> _logger;

    public AdminService(IUnitOfWork unitOfWork, IUserRepository userRepository, ILogger<AdminService> logger)
    {
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<SystemStatistics> GetSystemStatisticsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var courses = await _unitOfWork.Courses.ListAsync();
            var enrollments = await _unitOfWork.Enrollments.ListAsync();

            // TODO: Get user statistics from UserManager
            // This is a placeholder - you'll need to inject UserManager<ApplicationUser>
            var statistics = new SystemStatistics
            {
                TotalCourses = courses.Count,
                TotalEnrollments = enrollments.Count,
                // TotalUsers = await _userManager.Users.CountAsync(cancellationToken),
                // ActiveUsers = await _userManager.Users.CountAsync(u => u.IsActive, cancellationToken),
                UsersByRole = new Dictionary<string, int>
                {
                    { "Student", 0 },
                    { "Instructor", 0 },
                    { "Admin", 0 }
                },
                LastUpdated = DateTime.UtcNow
            };

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system statistics");
            throw;
        }
    }

    public async Task<UserManagementData> GetUserManagementDataAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        // TODO: Implement user pagination
        // This requires UserManager<ApplicationUser> to be injected
        return new UserManagementData
        {
            Users = new List<UserInfo>(),
            TotalCount = 0,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
    }

    public async Task<bool> UpdateUserRoleAsync(int userId, string role, CancellationToken cancellationToken = default)
    {
        // TODO: Implement role update using UserManager
        _logger.LogInformation("Updating user {UserId} role to {Role}", userId, role);
        return true;
    }

    public async Task<CourseManagementData> GetCourseManagementDataAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        try
        {
            var courses = await _unitOfWork.Courses.ListAsync();
            var courseInfos = courses.Select(c => new CourseInfo
            {
                Id = c.Id,
                Title = c.Title,
                Description = c.Description,
                CreatorName = "Unknown", // TODO: Get from UserRepository
                EnrollmentCount = c.Enrollments?.Count ?? 0,
                CreatedAt = c.CreatedAt,
                IsActive = true
            }).ToList();

            var totalCount = courseInfos.Count;
            var pagedCourses = courseInfos
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new CourseManagementData
            {
                Courses = pagedCourses,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting course management data");
            throw;
        }
    }

    public async Task<EnrollmentStatistics> GetEnrollmentStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var enrollments = await _unitOfWork.Enrollments.ListAsync();
            
            var filteredEnrollments = enrollments;
            if (startDate.HasValue)
            {
                filteredEnrollments = filteredEnrollments.Where(e => e.EnrolledAt >= startDate.Value).ToList();
            }
            if (endDate.HasValue)
            {
                filteredEnrollments = filteredEnrollments.Where(e => e.EnrolledAt <= endDate.Value).ToList();
            }

            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);
            var startOfWeek = now.AddDays(-(int)now.DayOfWeek);

            var statistics = new EnrollmentStatistics
            {
                TotalEnrollments = enrollments.Count,
                EnrollmentsThisMonth = enrollments.Count(e => e.EnrolledAt >= startOfMonth),
                EnrollmentsThisWeek = enrollments.Count(e => e.EnrolledAt >= startOfWeek),
                EnrollmentsByCourse = enrollments
                    .GroupBy(e => e.CourseId)
                    .ToDictionary(g => g.Key.ToString(), g => g.Count()),
                DailyEnrollments = enrollments
                    .GroupBy(e => e.EnrolledAt.Date)
                    .Select(g => new DailyEnrollment { Date = g.Key, Count = g.Count() })
                    .OrderBy(d => d.Date)
                    .ToList()
            };

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting enrollment statistics");
            throw;
        }
    }

    public async Task<GroupManagementData> GetGroupManagementDataAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        try
        {
            var groups = await _unitOfWork.Groups.ListAsync();
            var groupInfos = new List<GroupInfo>();

            foreach (var group in groups)
            {
                // Get creator name
                var creatorName = group.CreatedById.HasValue 
                    ? await _userRepository.GetUserFullNameAsync(group.CreatedById.Value) ?? "Unknown"
                    : "Unknown";

                groupInfos.Add(new GroupInfo
                {
                    Id = group.Id,
                    Name = group.Name,
                    Description = group.Description,
                    CreatorName = creatorName,
                    MemberCount = group.GroupUsers?.Count ?? 0,
                    CourseCount = group.CourseGroups?.Count ?? 0,
                    CreatedAt = group.CreatedAt
                });
            }

            var totalCount = groupInfos.Count;
            var pagedGroups = groupInfos
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new GroupManagementData
            {
                Groups = pagedGroups,
                TotalCount = totalCount,
                PageNumber = pageNumber,
                PageSize = pageSize
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting group management data");
            throw;
        }
    }
}

