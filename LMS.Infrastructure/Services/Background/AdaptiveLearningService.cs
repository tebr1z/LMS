using LMS.Application.Interfaces;
using LMS.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LMS.Infrastructure.Services.Background;

/// <summary>
/// Background service for daily adaptive learning analysis
/// </summary>
public class AdaptiveLearningBackgroundService : IHostedService, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AdaptiveLearningBackgroundService> _logger;
    private Timer? _timer;
    private const int IntervalHours = 24; // Run daily

    public AdaptiveLearningBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<AdaptiveLearningBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Adaptive Learning Background Service is starting.");

        // Run immediately on startup, then every 24 hours
        _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromHours(IntervalHours));

        return Task.CompletedTask;
    }

    private async void DoWork(object? state)
    {
        try
        {
            _logger.LogInformation("Starting daily adaptive learning analysis at {Time}", DateTime.UtcNow);

            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var adaptiveLearningService = scope.ServiceProvider.GetRequiredService<IAdaptiveLearningService>();
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

            // Get all students
            var allUsers = await userRepository.ListAsync();
            var students = allUsers.Where(u => u.Role == UserRole.Student).ToList();

            _logger.LogInformation("Analyzing learning levels for {Count} students", students.Count);

            // Analyze each student's performance
            int processedCount = 0;
            int errorCount = 0;

            foreach (var student in students)
            {
                try
                {
                    await adaptiveLearningService.AnalyzeAndUpdateLearningLevelAsync(student.Id);
                    processedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error analyzing learning level for student {StudentId}", student.Id);
                    errorCount++;
                }
            }

            _logger.LogInformation(
                "Completed daily adaptive learning analysis. Processed: {Processed}, Errors: {Errors}",
                processedCount, errorCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in adaptive learning background service");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Adaptive Learning Background Service is stopping.");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}

