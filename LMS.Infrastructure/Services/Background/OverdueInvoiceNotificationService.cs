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
/// Background service that runs daily to check for overdue invoices and send notifications
/// </summary>
public class OverdueInvoiceNotificationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OverdueInvoiceNotificationService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromDays(1); // Run daily
    private readonly TimeSpan _firstRunDelay = TimeSpan.FromMinutes(5); // Wait 5 minutes after startup

    public OverdueInvoiceNotificationService(
        IServiceProvider serviceProvider,
        ILogger<OverdueInvoiceNotificationService> logger)
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
                await CheckOverdueInvoicesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking overdue invoices");
            }

            // Wait for the next check interval
            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task CheckOverdueInvoicesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        _logger.LogInformation("Checking for overdue invoices...");

        // Get all overdue invoices
        var overdueInvoices = await unitOfWork.Invoices.GetOverdueInvoicesAsync();

        if (!overdueInvoices.Any())
        {
            _logger.LogInformation("No overdue invoices found");
            return;
        }

        _logger.LogInformation("Found {Count} overdue invoices", overdueInvoices.Count);

        // Get all Finance users
        var allUsers = await userRepository.ListAsync();
        var financeUsers = allUsers.Where(u => u.Role == UserRole.Finance || u.Role == UserRole.Admin || u.Role == UserRole.MasterAdmin).ToList();

        // Group invoices by student
        var invoicesByStudent = overdueInvoices.GroupBy(i => i.StudentId);

        foreach (var studentGroup in invoicesByStudent)
        {
            var studentId = studentGroup.Key;
            var studentInvoices = studentGroup.ToList();

            // Get student info
            var student = await userRepository.GetUserByIdAsync(studentId);
            if (student == null) continue;

            // Notify student
            var totalAmount = studentInvoices.Sum(i => i.Amount);
            var invoiceNumbers = string.Join(", ", studentInvoices.Select(i => i.InvoiceNumber));

            var studentNotification = new Notification
            {
                UserId = studentId,
                Title = "Overdue Invoice Reminder",
                Body = $"You have {studentInvoices.Count} overdue invoice(s) totaling {totalAmount} {studentInvoices.First().Currency}. Invoice(s): {invoiceNumbers}. Please make payment as soon as possible.",
                Type = "invoice_overdue",
                Channel = NotificationChannel.Both,
                Data = JsonSerializer.Serialize(new
                {
                    InvoiceIds = studentInvoices.Select(i => i.Id).ToList(),
                    InvoiceNumbers = studentInvoices.Select(i => i.InvoiceNumber).ToList(),
                    TotalAmount = totalAmount,
                    Currency = studentInvoices.First().Currency
                }),
                CreatedAt = DateTime.UtcNow
            };

            await unitOfWork.Notifications.AddAsync(studentNotification);

            // Send email and real-time notification
            try
            {
                var notificationMessage = new NotificationMessage
                {
                    Title = studentNotification.Title,
                    Message = studentNotification.Body,
                    Type = "warning",
                    Data = JsonSerializer.Deserialize<Dictionary<string, object>>(studentNotification.Data ?? "{}")
                };

                await notificationService.SendNotificationAsync(
                    studentId,
                    student.Email,
                    notificationMessage,
                    sendEmail: true,
                    sendRealTime: true,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to student {StudentId}", studentId);
            }

            // Notify Finance users
            foreach (var financeUser in financeUsers)
            {
                var financeNotification = new Notification
                {
                    UserId = financeUser.Id,
                    Title = $"Overdue Invoice: Student {student.FullName}",
                    Body = $"Student {student.FullName} has {studentInvoices.Count} overdue invoice(s) totaling {totalAmount} {studentInvoices.First().Currency}. Invoice(s): {invoiceNumbers}.",
                    Type = "invoice_overdue",
                    Channel = NotificationChannel.Both,
                    Data = JsonSerializer.Serialize(new
                    {
                        StudentId = studentId,
                        StudentName = student.FullName,
                        InvoiceIds = studentInvoices.Select(i => i.Id).ToList(),
                        InvoiceNumbers = studentInvoices.Select(i => i.InvoiceNumber).ToList(),
                        TotalAmount = totalAmount,
                        Currency = studentInvoices.First().Currency
                    }),
                    CreatedAt = DateTime.UtcNow
                };

                await unitOfWork.Notifications.AddAsync(financeNotification);

                try
                {
                    var notificationMessage = new NotificationMessage
                    {
                        Title = financeNotification.Title,
                        Message = financeNotification.Body,
                        Type = "warning",
                        Data = JsonSerializer.Deserialize<Dictionary<string, object>>(financeNotification.Data ?? "{}")
                    };

                    await notificationService.SendNotificationAsync(
                        financeUser.Id,
                        financeUser.Email,
                        notificationMessage,
                        sendEmail: true,
                        sendRealTime: true,
                        cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending notification to Finance user {UserId}", financeUser.Id);
                }
            }
        }

        await unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Completed processing overdue invoices");
    }
}

