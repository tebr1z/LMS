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
/// Background service that runs hourly to check for assignments approaching deadlines
/// Notifies students 24h before deadline and 3h before deadline
/// Also notifies teachers
/// </summary>
public class AssignmentDeadlineNotificationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AssignmentDeadlineNotificationService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1); // Run hourly
    private readonly TimeSpan _firstRunDelay = TimeSpan.FromMinutes(2); // Wait 2 minutes after startup

    public AssignmentDeadlineNotificationService(
        IServiceProvider serviceProvider,
        ILogger<AssignmentDeadlineNotificationService> logger)
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
                await CheckApproachingDeadlinesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking assignment deadlines");
            }

            // Wait for the next check interval
            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task CheckApproachingDeadlinesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        _logger.LogInformation("Checking for assignments approaching deadlines...");

        var now = DateTime.UtcNow;
        var in24Hours = now.AddHours(24);
        var in3Hours = now.AddHours(3);

        // Get all assignments with deadlines
        var allAssignments = await unitOfWork.Assignments.ListAsync();
        var assignmentsWithDeadlines = allAssignments
            .Where(a => a.Deadline.HasValue && a.Deadline.Value > now)
            .ToList();

        if (!assignmentsWithDeadlines.Any())
        {
            return;
        }

        // Get assignments approaching deadlines
        var assignments24h = assignmentsWithDeadlines
            .Where(a => a.Deadline!.Value <= in24Hours && a.Deadline.Value > now.AddHours(23))
            .ToList();

        var assignments3h = assignmentsWithDeadlines
            .Where(a => a.Deadline!.Value <= in3Hours && a.Deadline.Value > now.AddHours(2))
            .ToList();

        _logger.LogInformation("Found {Count24h} assignments due in 24h, {Count3h} assignments due in 3h",
            assignments24h.Count, assignments3h.Count);

        // Process 24-hour warnings
        foreach (var assignment in assignments24h)
        {
            await NotifyAssignmentDeadlineAsync(
                assignment,
                "24 hours",
                unitOfWork,
                notificationService,
                userRepository,
                cancellationToken);
        }

        // Process 3-hour warnings
        foreach (var assignment in assignments3h)
        {
            await NotifyAssignmentDeadlineAsync(
                assignment,
                "3 hours",
                unitOfWork,
                notificationService,
                userRepository,
                cancellationToken);
        }

        await unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Completed processing assignment deadline notifications");
    }

    private async Task NotifyAssignmentDeadlineAsync(
        Assignment assignment,
        string timeRemaining,
        IUnitOfWork unitOfWork,
        INotificationService notificationService,
        IUserRepository userRepository,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get all enrollments - need to find students enrolled in the course
            // Assignments can be linked to CourseId or CourseInstanceId
            var allEnrollments = await unitOfWork.Enrollments.ListAsync();
            
            List<int> studentIds = new List<int>();
            
            if (assignment.CourseId.HasValue)
            {
                // If assignment is linked to Course, get enrollments by CourseId
                var enrollments = allEnrollments
                    .Where(e => e.CourseId == assignment.CourseId.Value)
                    .ToList();
                studentIds = enrollments.Select(e => e.UserId).ToList();
            }
            else if (assignment.CourseInstanceId.HasValue)
            {
                // If assignment is linked to CourseInstance, we need to find students via Group
                // Get course instance and then find group members
                var courseInstance = await unitOfWork.CourseInstances.GetByIdAsync(assignment.CourseInstanceId.Value);
                if (courseInstance == null) return;

                // Get group members
                var allGroupUsers = await unitOfWork.GroupUsers.ListAsync();
                var groupMembers = allGroupUsers
                    .Where(gu => gu.GroupId == courseInstance.GroupId)
                    .Select(gu => gu.UserId)
                    .ToList();
                
                studentIds = groupMembers;
            }
            else
            {
                return; // Assignment not linked to course or instance
            }

            if (!studentIds.Any()) return;

            // Get all students
            var allUsers = await userRepository.ListAsync();
            var students = allUsers
                .Where(u => studentIds.Contains(u.Id) && u.Role == UserRole.Student)
                .ToList();

            // Get teacher
            var teacher = await userRepository.GetUserByIdAsync(assignment.CreatedById);

            // Get all submissions upfront to avoid multiple queries
            var allSubmissions = await unitOfWork.AssignmentSubmissions.ListAsync();
            
            // Notify students
            foreach (var student in students)
            {
                // Check if student has already submitted
                var submission = allSubmissions
                    .FirstOrDefault(s => s.AssignmentId == assignment.Id && s.StudentId == student.Id);

                if (submission != null) continue; // Skip if already submitted

                var studentNotification = new Notification
                {
                    UserId = student.Id,
                    Title = $"Assignment Due in {timeRemaining}",
                    Body = $"Assignment '{assignment.Title}' is due in {timeRemaining}. Deadline: {assignment.Deadline:yyyy-MM-dd HH:mm} UTC.",
                    Type = "assignment_deadline",
                    Channel = NotificationChannel.Both,
                    Data = JsonSerializer.Serialize(new
                    {
                        AssignmentId = assignment.Id,
                        AssignmentTitle = assignment.Title,
                        Deadline = assignment.Deadline,
                        TimeRemaining = timeRemaining,
                        CourseInstanceId = assignment.CourseInstanceId
                    }),
                    CreatedAt = DateTime.UtcNow
                };

                await unitOfWork.Notifications.AddAsync(studentNotification);

                try
                {
                    var notificationMessage = new NotificationMessage
                    {
                        Title = studentNotification.Title,
                        Message = studentNotification.Body,
                        Type = "info",
                        Data = JsonSerializer.Deserialize<Dictionary<string, object>>(studentNotification.Data ?? "{}")
                    };

                    await notificationService.SendNotificationAsync(
                        student.Id,
                        student.Email,
                        notificationMessage,
                        sendEmail: true,
                        sendRealTime: true,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending deadline notification to student {StudentId}", student.Id);
                }
            }

            // Notify teacher
            if (teacher != null)
            {
                var pendingSubmissions = students.Count - students.Count(s =>
                    allSubmissions.Any(sub => sub.AssignmentId == assignment.Id && sub.StudentId == s.Id));

                var teacherNotification = new Notification
                {
                    UserId = teacher.Id,
                    Title = $"Assignment Due in {timeRemaining}: {assignment.Title}",
                    Body = $"Assignment '{assignment.Title}' is due in {timeRemaining}. {pendingSubmissions} student(s) have not yet submitted.",
                    Type = "assignment_deadline",
                    Channel = NotificationChannel.Both,
                    Data = JsonSerializer.Serialize(new
                    {
                        AssignmentId = assignment.Id,
                        AssignmentTitle = assignment.Title,
                        Deadline = assignment.Deadline,
                        TimeRemaining = timeRemaining,
                        PendingSubmissions = pendingSubmissions,
                        CourseInstanceId = assignment.CourseInstanceId
                    }),
                    CreatedAt = DateTime.UtcNow
                };

                await unitOfWork.Notifications.AddAsync(teacherNotification);

                try
                {
                    var notificationMessage = new NotificationMessage
                    {
                        Title = teacherNotification.Title,
                        Message = teacherNotification.Body,
                        Type = "info",
                        Data = JsonSerializer.Deserialize<Dictionary<string, object>>(teacherNotification.Data ?? "{}")
                    };

                    await notificationService.SendNotificationAsync(
                        teacher.Id,
                        teacher.Email,
                        notificationMessage,
                        sendEmail: true,
                        sendRealTime: true,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending deadline notification to teacher {UserId}", teacher.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing assignment {AssignmentId}", assignment.Id);
        }
    }
}

