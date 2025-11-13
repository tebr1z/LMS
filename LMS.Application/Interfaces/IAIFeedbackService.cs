namespace LMS.Application.Interfaces;

/// <summary>
/// Service interface for AI-generated feedback
/// </summary>
public interface IAIFeedbackService
{
    /// <summary>
    /// Generate AI feedback for a submission
    /// </summary>
    Task<string> GenerateFeedbackAsync(string submissionText, CancellationToken cancellationToken = default);

    /// <summary>
    /// Process a submission and save AI feedback
    /// </summary>
    Task ProcessSubmissionAsync(int submissionId, CancellationToken cancellationToken = default);
}

