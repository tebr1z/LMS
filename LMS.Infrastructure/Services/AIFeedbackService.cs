using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json;

namespace LMS.Infrastructure.Services;

/// <summary>
/// Service implementation for AI-generated feedback using OpenAI API
/// </summary>
public class AIFeedbackService : IAIFeedbackService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AIFeedbackService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _model;

    public AIFeedbackService(
        IUnitOfWork unitOfWork,
        IConfiguration configuration,
        ILogger<AIFeedbackService> logger,
        HttpClient httpClient)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClient; // HttpClient is configured in DependencyInjection

        _model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";
    }

    public async Task<string> GenerateFeedbackAsync(string submissionText, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(submissionText))
        {
            return "Unable to generate feedback: submission text is empty.";
        }

        try
        {
            // Prepare the prompt
            var prompt = $"Analyze this student's answer and generate short feedback for the teacher (max 3 sentences). Focus on key strengths and areas for improvement.\n\nStudent Answer:\n{submissionText}";

            // Prepare the request body
            var requestBody = new
            {
                model = _model,
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = prompt
                    }
                },
                max_tokens = 200, // Limit to ~3 sentences
                temperature = 0.7
            };

            // Send request to OpenAI API
            var response = await _httpClient.PostAsJsonAsync(
                "chat/completions",
                requestBody,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
            var jsonResponse = JsonDocument.Parse(responseContent);

            // Extract the feedback text
            var feedback = jsonResponse.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "Unable to generate feedback.";

            return feedback.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI feedback");
            return $"Error generating feedback: {ex.Message}";
        }
    }

    public async Task ProcessSubmissionAsync(int submissionId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Check if feedback already exists
            if (await _unitOfWork.AssignmentFeedbackAI.ExistsForSubmissionAsync(submissionId, cancellationToken))
            {
                _logger.LogInformation("AI feedback already exists for submission {SubmissionId}", submissionId);
                return;
            }

            // Get submission
            var submission = await _unitOfWork.AssignmentSubmissions.GetByIdAsync(submissionId);
            if (submission == null)
            {
                _logger.LogWarning("Submission {SubmissionId} not found", submissionId);
                return;
            }

            // Skip if already scored or if no text content
            if (submission.Score.HasValue)
            {
                _logger.LogInformation("Submission {SubmissionId} already scored, skipping AI feedback", submissionId);
                return;
            }

            // Extract submission text
            string submissionText = string.Empty;

            if (!string.IsNullOrWhiteSpace(submission.AnswerText))
            {
                submissionText = submission.AnswerText;
            }
            else if (!string.IsNullOrWhiteSpace(submission.FileUrl))
            {
                // For PDF files, we would need to extract text
                // For now, we'll skip file-based submissions or implement basic text extraction
                // TODO: Implement PDF text extraction if needed
                _logger.LogWarning("Submission {SubmissionId} has file but no AnswerText. PDF extraction not implemented yet.", submissionId);
                return;
            }
            else
            {
                _logger.LogWarning("Submission {SubmissionId} has no text content to analyze", submissionId);
                return;
            }

            // Generate AI feedback
            var feedbackText = await GenerateFeedbackAsync(submissionText, cancellationToken);

            // Save feedback
            var feedback = new AssignmentFeedbackAI
            {
                SubmissionId = submissionId,
                AIComment = feedbackText,
                ConfidenceScore = null, // OpenAI doesn't provide confidence scores in chat completions
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.AssignmentFeedbackAI.AddAsync(feedback);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "Generated AI feedback for submission {SubmissionId}. Feedback length: {Length}",
                submissionId, feedbackText.Length);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing submission {SubmissionId} for AI feedback", submissionId);
            // Don't throw - this should not break the background service
        }
    }
}

