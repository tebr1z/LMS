using LMS.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LMS.Infrastructure.Services.Background;

/// <summary>
/// Background service for AI feedback generation (runs every 10 minutes)
/// </summary>
public class AIFeedbackBackgroundService : IHostedService, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AIFeedbackBackgroundService> _logger;
    private Timer? _timer;
    private const int IntervalMinutes = 10; // Run every 10 minutes

    public AIFeedbackBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<AIFeedbackBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("AI Feedback Background Service is starting.");

        // Run immediately on startup, then every 10 minutes
        _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromMinutes(IntervalMinutes));

        return Task.CompletedTask;
    }

    private async void DoWork(object? state)
    {
        try
        {
            _logger.LogInformation("Starting AI feedback generation cycle at {Time}", DateTime.UtcNow);

            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var aiFeedbackService = scope.ServiceProvider.GetRequiredService<IAIFeedbackService>();

            // Find submissions that need AI feedback:
            // - Score is null (not yet graded)
            // - No AI feedback exists yet
            var allSubmissions = await unitOfWork.AssignmentSubmissions.ListAsync();
            var allFeedback = await unitOfWork.AssignmentFeedbackAI.ListAsync();

            var feedbackSubmissionIds = allFeedback.Select(f => f.SubmissionId).ToHashSet();

            var submissionsNeedingFeedback = allSubmissions
                .Where(s => !s.Score.HasValue && !feedbackSubmissionIds.Contains(s.Id))
                .Where(s => !string.IsNullOrWhiteSpace(s.AnswerText)) // Only process text submissions for now
                .Take(10) // Process max 10 submissions per cycle to avoid rate limits
                .ToList();

            if (!submissionsNeedingFeedback.Any())
            {
                _logger.LogInformation("No submissions need AI feedback at this time.");
                return;
            }

            _logger.LogInformation("Processing {Count} submissions for AI feedback", submissionsNeedingFeedback.Count);

            int processedCount = 0;
            int errorCount = 0;

            foreach (var submission in submissionsNeedingFeedback)
            {
                try
                {
                    await aiFeedbackService.ProcessSubmissionAsync(submission.Id);
                    processedCount++;

                    // Add a small delay to avoid rate limiting
                    await Task.Delay(1000, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing submission {SubmissionId} for AI feedback", submission.Id);
                    errorCount++;
                }
            }

            _logger.LogInformation(
                "Completed AI feedback generation cycle. Processed: {Processed}, Errors: {Errors}",
                processedCount, errorCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AI feedback background service");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("AI Feedback Background Service is stopping.");
        _timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}

