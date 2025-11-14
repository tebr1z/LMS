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
/// Background service that runs every hour to evaluate notification rules and create notifications
/// </summary>
public class NotificationSchedulerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationSchedulerService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1); // Run every hour
    private readonly TimeSpan _firstRunDelay = TimeSpan.FromMinutes(5); // Wait 5 minutes after startup

    public NotificationSchedulerService(
        IServiceProvider serviceProvider,
        ILogger<NotificationSchedulerService> logger)
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
                await EvaluateRulesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating notification rules");
            }

            // Wait for the next check interval
            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task EvaluateRulesAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting notification rule evaluation at {Time}", DateTime.UtcNow);

            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var notificationService = scope.ServiceProvider.GetService<INotificationService>();
            var realTimeNotificationService = scope.ServiceProvider.GetRequiredService<IRealTimeNotificationService>();

            // Get all active rules
            var rules = await unitOfWork.NotificationRules.GetActiveRulesAsync(cancellationToken);

            if (!rules.Any())
            {
                _logger.LogInformation("No active notification rules found.");
                return;
            }

            _logger.LogInformation("Evaluating {Count} notification rules", rules.Count);

            int notificationsCreated = 0;
            int errorsCount = 0;

            foreach (var rule in rules)
            {
                try
                {
                    var usersToNotify = await EvaluateRuleAsync(rule, unitOfWork, userRepository, cancellationToken);

                    if (usersToNotify.Any())
                    {
                        _logger.LogInformation("Rule '{RuleName}' matched {Count} users", rule.Name, usersToNotify.Count);

                        foreach (var userInfo in usersToNotify)
                        {
                            try
                            {
                                // Create notification in database
                                var notification = new Notification
                                {
                                    UserId = userInfo.UserId,
                                    Title = BuildTitle(rule, userInfo),
                                    Body = BuildMessage(rule, userInfo),
                                    Type = rule.Type ?? "rule_based",
                                    IsRead = false,
                                    CreatedAt = DateTime.UtcNow,
                                    Channel = NotificationChannel.InApp,
                                    Data = userInfo.DataJson // Store additional context data
                                };

                                await unitOfWork.Notifications.AddAsync(notification);
                                await unitOfWork.SaveChangesAsync(cancellationToken);

                                // Send real-time notification via SignalR
                                var notificationMessage = new NotificationMessage
                                {
                                    Title = notification.Title,
                                    Message = notification.Body,
                                    Type = notification.Type ?? "info",
                                    CreatedAt = notification.CreatedAt
                                };

                                // Parse Data JSON to Dictionary if available
                                if (!string.IsNullOrEmpty(notification.Data))
                                {
                                    try
                                    {
                                        var dataDict = JsonSerializer.Deserialize<Dictionary<string, object>>(notification.Data);
                                        notificationMessage.Data = dataDict;
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogWarning(ex, "Error parsing notification data JSON for user {UserId}", userInfo.UserId);
                                    }
                                }

                                await realTimeNotificationService.SendToUserAsync(
                                    userInfo.UserId,
                                    notificationMessage,
                                    cancellationToken);

                                notificationsCreated++;
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error creating notification for user {UserId} with rule {RuleId}", userInfo.UserId, rule.Id);
                                errorsCount++;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error evaluating rule {RuleId}: {RuleName}", rule.Id, rule.Name);
                    errorsCount++;
                }
            }

            _logger.LogInformation(
                "Completed notification rule evaluation. Notifications created: {Created}, Errors: {Errors}",
                notificationsCreated, errorsCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in notification rule evaluation");
            throw;
        }
    }

    private async Task<List<UserMatchInfo>> EvaluateRuleAsync(
        NotificationRule rule,
        IUnitOfWork unitOfWork,
        IUserRepository userRepository,
        CancellationToken cancellationToken)
    {
        var usersToNotify = new List<UserMatchInfo>();

        try
        {
            // Parse condition JSON
            var condition = JsonSerializer.Deserialize<RuleCondition>(rule.ConditionJson);
            if (condition == null)
            {
                _logger.LogWarning("Rule {RuleId} has invalid condition JSON", rule.Id);
                return usersToNotify;
            }

            // Get all users with the target role
            var allUsers = await userRepository.ListAsync();
            var targetUsers = allUsers
                .Where(u => u.Role == rule.TargetRole)
                .ToList();

            switch (condition.Type.ToLowerInvariant())
            {
                case "inactive_days":
                    usersToNotify = await EvaluateInactiveDaysRule(condition, targetUsers, unitOfWork, cancellationToken);
                    break;

                case "deadline_approaching":
                    usersToNotify = await EvaluateDeadlineApproachingRule(condition, targetUsers, unitOfWork, cancellationToken);
                    break;

                case "low_average_score":
                    usersToNotify = await EvaluateLowAverageScoreRule(condition, targetUsers, unitOfWork, cancellationToken);
                    break;

                default:
                    _logger.LogWarning("Unknown rule condition type: {Type}", condition.Type);
                    break;
            }
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error parsing condition JSON for rule {RuleId}", rule.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating rule {RuleId}", rule.Id);
        }

        return usersToNotify;
    }

    private async Task<List<UserMatchInfo>> EvaluateInactiveDaysRule(
        RuleCondition condition,
        List<Domain.Entities.ApplicationUser> targetUsers,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var usersToNotify = new List<UserMatchInfo>();
        var thresholdDays = condition.Threshold ?? 3;
        var cutoffDate = DateTime.UtcNow.AddDays(-thresholdDays);

        // Get all submissions and quiz sessions to check last activity
        var allSubmissions = await unitOfWork.AssignmentSubmissions.ListAsync();
        var allQuizSessions = await unitOfWork.QuizSessions.ListAsync();

        foreach (var user in targetUsers)
        {
            // Find last activity (submission or quiz session)
            var lastSubmission = allSubmissions
                .Where(s => s.StudentId == user.Id && s.SubmittedAt.HasValue)
                .OrderByDescending(s => s.SubmittedAt)
                .FirstOrDefault();

            var lastQuizSession = allQuizSessions
                .Where(qs => qs.StudentId == user.Id && qs.StartedAt.HasValue)
                .OrderByDescending(qs => qs.StartedAt)
                .FirstOrDefault();

            DateTime? lastActivity = null;

            if (lastSubmission?.SubmittedAt != null && lastQuizSession?.StartedAt != null)
            {
                lastActivity = lastSubmission.SubmittedAt.Value > lastQuizSession.StartedAt.Value
                    ? lastSubmission.SubmittedAt.Value
                    : lastQuizSession.StartedAt.Value;
            }
            else if (lastSubmission?.SubmittedAt != null)
            {
                lastActivity = lastSubmission.SubmittedAt.Value;
            }
            else if (lastQuizSession?.StartedAt != null)
            {
                lastActivity = lastQuizSession.StartedAt.Value;
            }

            // Check if user is inactive
            if (!lastActivity.HasValue || lastActivity.Value < cutoffDate)
            {
                var daysInactive = lastActivity.HasValue
                    ? (int)(DateTime.UtcNow - lastActivity.Value).TotalDays
                    : (int)(DateTime.UtcNow - user.CreatedAt).TotalDays;

                usersToNotify.Add(new UserMatchInfo
                {
                    UserId = user.Id,
                    DataJson = JsonSerializer.Serialize(new { daysInactive, lastActivity = lastActivity?.ToString("O") })
                });
            }
        }

        return usersToNotify;
    }

    private async Task<List<UserMatchInfo>> EvaluateDeadlineApproachingRule(
        RuleCondition condition,
        List<Domain.Entities.ApplicationUser> targetUsers,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var usersToNotify = new List<UserMatchInfo>();
        var thresholdHours = condition.Threshold ?? 24;
        var deadlineCutoff = DateTime.UtcNow.AddHours(thresholdHours);

        // Get all assignments with deadlines approaching
        var allAssignments = await unitOfWork.Assignments.ListAsync();
        var allSubmissions = await unitOfWork.AssignmentSubmissions.ListAsync();

        var approachingAssignments = allAssignments
            .Where(a => a.Deadline.HasValue &&
                       a.Deadline.Value > DateTime.UtcNow &&
                       a.Deadline.Value <= deadlineCutoff &&
                       a.IsPublished)
            .ToList();

        // For students: check if they haven't submitted
        if (targetUsers.Any() && targetUsers.First().Role == UserRole.Student)
        {
            foreach (var assignment in approachingAssignments)
            {
                // Get students in the assignment's group
                var groupUsers = await unitOfWork.GroupUsers.GetGroupUsersByGroupIdAsync(assignment.GroupId ?? 0);
                var studentIds = groupUsers
                    .Where(gu => gu.Role == GroupRole.Student)
                    .Select(gu => gu.UserId)
                    .ToList();

                foreach (var studentId in studentIds)
                {
                    // Check if student has submitted
                    var hasSubmitted = allSubmissions.Any(s => s.AssignmentId == assignment.Id && s.StudentId == studentId);

                    if (!hasSubmitted)
                    {
                        var hoursUntilDeadline = (int)(assignment.Deadline!.Value - DateTime.UtcNow).TotalHours;

                        usersToNotify.Add(new UserMatchInfo
                        {
                            UserId = studentId,
                            DataJson = JsonSerializer.Serialize(new
                            {
                                assignmentId = assignment.Id,
                                assignmentTitle = assignment.Title,
                                deadline = assignment.Deadline.Value.ToString("O"),
                                hoursUntilDeadline
                            })
                        });
                    }
                }
            }
        }
        else if (targetUsers.Any() && targetUsers.First().Role == UserRole.Teacher)
        {
            // For teachers: notify about all approaching deadlines
            var teacherIds = targetUsers.Select(u => u.Id).ToList();

            foreach (var assignment in approachingAssignments)
            {
                if (teacherIds.Contains(assignment.CreatedById))
                {
                    var hoursUntilDeadline = (int)(assignment.Deadline!.Value - DateTime.UtcNow).TotalHours;

                    usersToNotify.Add(new UserMatchInfo
                    {
                        UserId = assignment.CreatedById,
                        DataJson = JsonSerializer.Serialize(new
                        {
                            assignmentId = assignment.Id,
                            assignmentTitle = assignment.Title,
                            deadline = assignment.Deadline.Value.ToString("O"),
                            hoursUntilDeadline
                        })
                    });
                }
            }
        }

        return usersToNotify;
    }

    private async Task<List<UserMatchInfo>> EvaluateLowAverageScoreRule(
        RuleCondition condition,
        List<Domain.Entities.ApplicationUser> targetUsers,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        var usersToNotify = new List<UserMatchInfo>();
        var thresholdScore = condition.Threshold ?? 60;

        // Get all submissions
        var allSubmissions = await unitOfWork.AssignmentSubmissions.ListAsync();

        foreach (var user in targetUsers)
        {
            // Get user's submissions with scores
            var userSubmissions = allSubmissions
                .Where(s => s.StudentId == user.Id && s.Score.HasValue)
                .ToList();

            if (userSubmissions.Any())
            {
                var averageScore = userSubmissions.Average(s => s.PercentageScore);

                if (averageScore < thresholdScore)
                {
                    usersToNotify.Add(new UserMatchInfo
                    {
                        UserId = user.Id,
                        DataJson = JsonSerializer.Serialize(new
                        {
                            averageScore = Math.Round(averageScore, 2),
                            thresholdScore,
                            submissionCount = userSubmissions.Count
                        })
                    });
                }
            }
        }

        return usersToNotify;
    }

    private string BuildTitle(NotificationRule rule, UserMatchInfo userInfo)
    {
        // Simple title - could be enhanced with templating
        return rule.Type switch
        {
            "inactivity" => "Inactivity Alert",
            "deadline_approaching" => "Upcoming Deadline",
            "low_performance" => "Performance Alert",
            _ => rule.Name
        };
    }

    private string BuildMessage(NotificationRule rule, UserMatchInfo userInfo)
    {
        var message = rule.MessageTemplate;

        // Replace placeholders with actual values
        try
        {
            if (!string.IsNullOrEmpty(userInfo.DataJson))
            {
                var data = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(userInfo.DataJson);
                if (data != null)
                {
                    foreach (var kvp in data)
                    {
                        var value = kvp.Value.ValueKind switch
                        {
                            JsonValueKind.String => kvp.Value.GetString() ?? "",
                            JsonValueKind.Number => kvp.Value.GetDecimal().ToString(),
                            JsonValueKind.True => "true",
                            JsonValueKind.False => "false",
                            _ => kvp.Value.ToString()
                        };

                        message = message.Replace($"{{{kvp.Key}}}", value);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error building message for rule {RuleId}", rule.Id);
        }

        return message;
    }

    private class RuleCondition
    {
        public string Type { get; set; } = string.Empty; // e.g., "inactive_days", "deadline_approaching", "low_average_score"
        public int? Threshold { get; set; } // e.g., 3 (days), 24 (hours), 60 (score)
        public UserRole? TargetRole { get; set; }
    }

    private class UserMatchInfo
    {
        public int UserId { get; set; }
        public string? DataJson { get; set; } // Additional context data as JSON
    }
}

