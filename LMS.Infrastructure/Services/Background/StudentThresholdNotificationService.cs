using LMS.Application.Interfaces;
using LMS.Application.Interfaces.Notifications;
using LMS.Domain.Entities;
using LMS.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LMS.Infrastructure.Services.Background;

/// <summary>
/// Background service that checks for students falling below thresholds
/// Creates StudentFlag and notifies StudentOffice
/// </summary>
public class StudentThresholdNotificationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StudentThresholdNotificationService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(6); // Run every 6 hours
    private readonly TimeSpan _firstRunDelay = TimeSpan.FromMinutes(10); // Wait 10 minutes after startup

    // Thresholds (can be moved to settings)
    private const decimal AttendanceThreshold = 75m; // Minimum attendance percentage
    private const decimal AverageScoreThreshold = 60m; // Minimum average score
    private const int MissingAssignmentsThreshold = 3; // Maximum missing assignments

    public StudentThresholdNotificationService(
        IServiceProvider serviceProvider,
        ILogger<StudentThresholdNotificationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait before first run to allow app to fully start
        await Task.Delay(_firstRunDelay, stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckStudentThresholdsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking student thresholds");
            }

            // Wait for the next check interval
            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task CheckStudentThresholdsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        _logger.LogInformation("Checking student thresholds...");

        // Get all students
        var allUsers = await userRepository.ListAsync();
        var students = allUsers.Where(u => u.Role == UserRole.Student).ToList();

        // Get StudentOffice users
        var studentOfficeUsers = allUsers
            .Where(u => u.Role == UserRole.StudentOffice || u.Role == UserRole.Admin || u.Role == UserRole.MasterAdmin)
            .ToList();

        if (!studentOfficeUsers.Any())
        {
            _logger.LogWarning("No StudentOffice users found");
            return;
        }

        foreach (var student in students)
        {
            try
            {
                var flags = new List<string>();

                // Check attendance
                var attendanceFlag = await CheckAttendanceThresholdAsync(student.Id, unitOfWork);
                if (!string.IsNullOrEmpty(attendanceFlag))
                {
                    flags.Add(attendanceFlag);
                }

                // Check average score
                var scoreFlag = await CheckAverageScoreThresholdAsync(student.Id, unitOfWork);
                if (!string.IsNullOrEmpty(scoreFlag))
                {
                    flags.Add(scoreFlag);
                }

                // Check missing assignments
                var assignmentFlag = await CheckMissingAssignmentsThresholdAsync(student.Id, unitOfWork);
                if (!string.IsNullOrEmpty(assignmentFlag))
                {
                    flags.Add(assignmentFlag);
                }

                if (flags.Any())
                {
                    var reason = string.Join("; ", flags);
                    var recommendedAction = "Review student progress and contact student if necessary.";

                    // Check if flag already exists and is not resolved
                    var allFlags = await unitOfWork.StudentFlags.ListAsync();
                    var existingFlag = allFlags
                        .FirstOrDefault(f => f.StudentId == student.Id && 
                                            f.Reason == reason && 
                                            !f.IsResolved);

                    if (existingFlag == null)
                    {
                        // Create new StudentFlag (assuming StudentOffice user exists)
                        var firstStudentOfficeUser = studentOfficeUsers.First();
                        
                        var studentFlag = new StudentFlag
                        {
                            StudentId = student.Id,
                            CreatedById = firstStudentOfficeUser.Id,
                            Reason = reason,
                            RecommendedAction = recommendedAction,
                            IsResolved = false,
                            CreatedAt = DateTime.UtcNow
                        };

                        await unitOfWork.StudentFlags.AddAsync(studentFlag);
                        await unitOfWork.SaveChangesAsync();

                        // Notify StudentOffice users
                        foreach (var officeUser in studentOfficeUsers)
                        {
                            var notification = new Notification
                            {
                                UserId = officeUser.Id,
                                Title = $"Student Flag Created: {student.FullName}",
                                Body = $"Student {student.FullName} has been flagged. Reason: {reason}. Recommended Action: {recommendedAction}",
                                Type = "student_flag",
                                Channel = NotificationChannel.Both,
                                Data = JsonSerializer.Serialize(new
                                {
                                    StudentId = student.Id,
                                    StudentName = student.FullName,
                                    FlagId = studentFlag.Id,
                                    Reason = reason,
                                    RecommendedAction = recommendedAction
                                }),
                                CreatedAt = DateTime.UtcNow
                            };

                            await unitOfWork.Notifications.AddAsync(notification);

                            try
                            {
                                var notificationMessage = new NotificationMessage
                                {
                                    Title = notification.Title,
                                    Message = notification.Body,
                                    Type = "warning",
                                    Data = JsonSerializer.Deserialize<Dictionary<string, object>>(notification.Data ?? "{}")
                                };

                                await notificationService.SendNotificationAsync(
                                    officeUser.Id,
                                    officeUser.Email,
                                    notificationMessage,
                                    sendEmail: true,
                                    sendRealTime: true,
                                    cancellationToken);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error sending notification to StudentOffice user {UserId}", officeUser.Id);
                            }
                        }

                        _logger.LogInformation("Created StudentFlag for student {StudentId}: {Reason}", student.Id, reason);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking thresholds for student {StudentId}", student.Id);
            }
        }

        await unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Completed checking student thresholds");
    }

    private async Task<string?> CheckAttendanceThresholdAsync(int studentId, IUnitOfWork unitOfWork)
    {
        // Get student's attendance records
        var allAttendances = await unitOfWork.Attendances.ListAsync();
        var studentAttendances = allAttendances
            .Where(a => a.StudentId == studentId)
            .ToList();

        if (!studentAttendances.Any()) return null;

        var totalSessions = studentAttendances.Count;
        var presentCount = studentAttendances.Count(a => a.Present);
        var attendancePercent = (decimal)presentCount / totalSessions * 100;

        if (attendancePercent < AttendanceThreshold)
        {
            return $"Low attendance: {attendancePercent:F1}% (threshold: {AttendanceThreshold}%)";
        }

        return null;
    }

    private async Task<string?> CheckAverageScoreThresholdAsync(int studentId, IUnitOfWork unitOfWork)
    {
        // Get student stats
        var stats = await unitOfWork.StudentStats.GetByStudentIdAsync(studentId, courseInstanceId: null);
        
        if (stats != null && stats.AveragePercent < AverageScoreThreshold && stats.AssignmentsCompletedCount > 0)
        {
            return $"Low average score: {stats.AveragePercent:F1}% (threshold: {AverageScoreThreshold}%)";
        }

        return null;
    }

    private async Task<string?> CheckMissingAssignmentsThresholdAsync(int studentId, IUnitOfWork unitOfWork)
    {
        // Get all assignments
        var allAssignments = await unitOfWork.Assignments.ListAsync();
        var allSubmissions = await unitOfWork.AssignmentSubmissions.ListAsync();

        // Get assignments that have deadlines and are past due
        var now = DateTime.UtcNow;
        var pastDueAssignments = allAssignments
            .Where(a => a.Deadline.HasValue && a.Deadline.Value < now)
            .ToList();

        if (!pastDueAssignments.Any()) return null;

        // Check which assignments student hasn't submitted
        var missingAssignments = pastDueAssignments
            .Where(a =>
            {
                var submission = allSubmissions
                    .FirstOrDefault(s => s.AssignmentId == a.Id && s.StudentId == studentId);
                return submission == null;
            })
            .ToList();

        if (missingAssignments.Count >= MissingAssignmentsThreshold)
        {
            return $"Missing {missingAssignments.Count} assignment(s) past due (threshold: {MissingAssignmentsThreshold})";
        }

        return null;
    }
}

