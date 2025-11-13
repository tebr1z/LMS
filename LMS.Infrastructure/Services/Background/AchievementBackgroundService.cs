using LMS.Application.Interfaces;
using LMS.Domain.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LMS.Infrastructure.Services.Background;

/// <summary>
/// Background service for daily achievement checks
/// </summary>
public class AchievementBackgroundService : IHostedService, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AchievementBackgroundService> _logger;
    private Timer? _timer;
    private const int IntervalHours = 24; // Run daily

    public AchievementBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<AchievementBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Achievement Background Service is starting.");

        // Run immediately on startup, then every 24 hours
        _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromHours(IntervalHours));

        return Task.CompletedTask;
    }

    private async void DoWork(object? state)
    {
        try
        {
            _logger.LogInformation("Starting daily achievement checks at {Time}", DateTime.UtcNow.ToString("O"));

            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var achievementEngine = scope.ServiceProvider.GetRequiredService<IAchievementEngine>();
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();

            // Get all students
            var allUsers = await userRepository.ListAsync();
            var students = allUsers.Where(u => u.Role == UserRole.Student).ToList();

            _logger.LogInformation("Checking achievements for {Count} students", students.Count);

            // Check achievements for each student
            int processedCount = 0;
            int errorCount = 0;

            foreach (var student in students)
            {
                try
                {
                    await achievementEngine.CheckAndAwardAchievementsAsync(student.Id);
                    processedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error checking achievements for student {StudentId}", student.Id);
                    errorCount++;
                }
            }

            _logger.LogInformation(
                "Completed daily achievement checks. Processed: {Processed}, Errors: {Errors}",
                processedCount, errorCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in achievement background service");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Achievement Background Service is stopping.");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}


