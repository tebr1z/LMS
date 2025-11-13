using LMS.Application.Interfaces;
using LMS.Domain.Entities;
using LMS.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using System.Text.Json;

namespace LMS.API.Hubs;

[Authorize]
public class QuizHub : Hub
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;

    public QuizHub(IUnitOfWork unitOfWork, IUserRepository userRepository)
    {
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
    }

    public override async Task OnConnectedAsync()
    {
        // Authenticate via JWT in query string or header and record connection id -> map to user
        var userId = GetUserId();
        if (userId.HasValue)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId.Value}");
        }
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // Remove connection mapping
        var userId = GetUserId();
        if (userId.HasValue)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId.Value}");
        }
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Host (teacher) starts quiz for AssignmentTestId
    /// Server sends "QuizStarted" with quizId, questions
    /// </summary>
    public async Task StartQuiz(int assignmentTestId)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var user = await _userRepository.GetUserByIdAsync(userId.Value);
        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid user.");
        }

        // Only Teacher or Admin can start quiz
        if (user.Role != UserRole.Teacher && user.Role != UserRole.MasterAdmin && user.Role != UserRole.Admin)
        {
            throw new UnauthorizedAccessException("Only Teacher or Admin can start quizzes.");
        }

        // Get assignment and verify it's a Test type
        var assignment = await _unitOfWork.Assignments.GetByIdAsync(assignmentTestId);
        if (assignment == null)
        {
            throw new InvalidOperationException($"Assignment with ID {assignmentTestId} not found.");
        }

        if (assignment.AssignmentType != AssignmentType.Test)
        {
            throw new InvalidOperationException("Assignment must be of type Test to start a quiz.");
        }

        // Get Quiz from database
        var quiz = await _unitOfWork.Quizzes.GetByAssignmentIdAsync(assignmentTestId);
        if (quiz == null)
        {
            throw new InvalidOperationException($"Quiz not found for Assignment with ID {assignmentTestId}. Please create a quiz first.");
        }

        // Load questions
        quiz = await _unitOfWork.Quizzes.GetQuizWithQuestionsAsync(quiz.Id);
        if (quiz == null || !quiz.Questions.Any())
        {
            throw new InvalidOperationException("Quiz has no questions.");
        }

        // Send "QuizStarted" to all students in the group/course
        var groupName = assignment.GroupId.HasValue ? $"group_{assignment.GroupId}" : $"quiz_{quiz.QuizId}";
        var questionsForStudents = quiz.Questions
            .OrderBy(q => q.Order)
            .Select(q => new
            {
                q.Id,
                q.Text,
                Options = JsonSerializer.Deserialize<List<string>>(q.Options) ?? new List<string>(),
                // Don't send correct answer to students
            })
            .ToList();

        await Clients.Group(groupName).SendAsync("QuizStarted", new
        {
            QuizId = quiz.QuizId,
            AssignmentId = assignment.Id,
            Questions = questionsForStudents,
            TimeLimitSeconds = quiz.TimeLimitSeconds
        });

        await Clients.Caller.SendAsync("QuizHostStarted", new
        {
            QuizId = quiz.QuizId,
            AssignmentId = assignment.Id,
            Questions = questionsForStudents
        });
    }

    /// <summary>
    /// Students join using JoinQuiz(quizId) - creates QuizSession
    /// </summary>
    public async Task JoinQuiz(string quizId)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        var user = await _userRepository.GetUserByIdAsync(userId.Value);
        if (user == null)
        {
            throw new UnauthorizedAccessException("Invalid user.");
        }

        // Only Students can join quiz
        if (user.Role != UserRole.Student)
        {
            throw new UnauthorizedAccessException("Only Students can join quizzes.");
        }

        // Find quiz by QuizId
        var allQuizzes = await _unitOfWork.Quizzes.ListAsync();
        var quiz = allQuizzes.FirstOrDefault(q => q.QuizId == quizId);
        if (quiz == null)
        {
            throw new InvalidOperationException($"Quiz with ID {quizId} not found.");
        }

        quiz = await _unitOfWork.Quizzes.GetQuizWithQuestionsAsync(quiz.Id);
        if (quiz == null)
        {
            throw new InvalidOperationException($"Quiz with ID {quizId} not found.");
        }

        var assignment = await _unitOfWork.Assignments.GetByIdAsync(quiz.AssignmentId);
        if (assignment == null)
        {
            throw new InvalidOperationException($"Assignment not found for quiz.");
        }

        // Check if student belongs to the group (if quiz is group-specific)
        if (assignment.GroupId.HasValue)
        {
            var isUserInGroup = await _unitOfWork.Groups.IsUserInGroupAsync(assignment.GroupId.Value, userId.Value);
            if (!isUserInGroup)
            {
                throw new UnauthorizedAccessException("Student is not a member of the group for this quiz.");
            }
        }

        // Check if there's an active session (if not completed)
        var activeSession = await _unitOfWork.QuizSessions.GetActiveSessionByStudentAndQuizAsync(userId.Value, quiz.Id);
        if (activeSession != null && !activeSession.IsCompleted)
        {
            // Student already has an active session, return it
            var questionsForStudent = GetQuestionsForStudent(quiz, activeSession);
            await Clients.Caller.SendAsync("JoinedQuiz", new
            {
                QuizId = quiz.QuizId,
                AssignmentId = assignment.Id,
                Questions = questionsForStudent,
                SessionId = activeSession.Id,
                StartedAt = activeSession.StartedAt,
                ExpiresAt = activeSession.ExpiresAt,
                TimeLimitSeconds = quiz.TimeLimitSeconds
            });
            return;
        }

        // Create new QuizSession
        var now = DateTime.UtcNow;
        var expiresAt = quiz.TimeLimitSeconds.HasValue
            ? now.AddSeconds(quiz.TimeLimitSeconds.Value)
            : (DateTime?)null;

        var session = new QuizSession
        {
            StudentId = userId.Value,
            QuizId = quiz.Id,
            AssignmentId = quiz.AssignmentId,
            StartedAt = now,
            ExpiresAt = expiresAt,
            IsCompleted = false,
            TotalPoints = quiz.Questions.Sum(q => q.Points),
            PointsAwarded = 0,
            CreatedAt = now
        };

        await _unitOfWork.QuizSessions.AddAsync(session);
        await _unitOfWork.SaveChangesAsync();

        // Add student to quiz group
        var quizGroupName = $"quiz_{quiz.QuizId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, quizGroupName);

        var questions = GetQuestionsForStudent(quiz, session);

        await Clients.Caller.SendAsync("JoinedQuiz", new
        {
            QuizId = quiz.QuizId,
            AssignmentId = assignment.Id,
            Questions = questions,
            SessionId = session.Id,
            StartedAt = session.StartedAt,
            ExpiresAt = session.ExpiresAt,
            TimeLimitSeconds = quiz.TimeLimitSeconds
        });
    }

    /// <summary>
    /// StudentAnswer(quizId, questionId, answer) -> server evaluates and stores QuizResponse
    /// </summary>
    public async Task StudentAnswer(string quizId, int questionId, string answer, string? answerText = null)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        // Find quiz
        var allQuizzes = await _unitOfWork.Quizzes.ListAsync();
        var quiz = allQuizzes.FirstOrDefault(q => q.QuizId == quizId);
        if (quiz == null)
        {
            throw new InvalidOperationException($"Quiz with ID {quizId} not found.");
        }

        quiz = await _unitOfWork.Quizzes.GetQuizWithQuestionsAsync(quiz.Id);
        if (quiz == null)
        {
            throw new InvalidOperationException($"Quiz with ID {quizId} not found.");
        }

        // Get active session
        var session = await _unitOfWork.QuizSessions.GetActiveSessionByStudentAndQuizAsync(userId.Value, quiz.Id);
        if (session == null || session.IsCompleted)
        {
            throw new InvalidOperationException("Student has not started this quiz or quiz is already completed.");
        }

        // Check time limit
        if (session.ExpiresAt.HasValue && DateTime.UtcNow > session.ExpiresAt.Value)
        {
            throw new InvalidOperationException("Quiz time limit has expired.");
        }

        // Get question
        var question = quiz.Questions.FirstOrDefault(q => q.Id == questionId);
        if (question == null)
        {
            throw new InvalidOperationException($"Question with ID {questionId} not found.");
        }

        // Check if response already exists
        var existingResponses = await _unitOfWork.QuizResponses.ListAsync();
        var existingResponse = existingResponses
            .FirstOrDefault(r => r.QuizSessionId == session.Id && r.QuizQuestionId == questionId);

        // Evaluate answer (support multiple correct answers separated by comma)
        var isCorrect = EvaluateAnswer(answer, question.CorrectAnswer);
        var pointsAwarded = isCorrect ? question.Points : 0;

        if (existingResponse != null)
        {
            // Update existing response
            existingResponse.SelectedOption = answer;
            existingResponse.AnswerText = answerText;
            existingResponse.IsCorrect = isCorrect;
            existingResponse.PointsAwarded = pointsAwarded;
            existingResponse.AnsweredAt = DateTime.UtcNow;
            existingResponse.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.QuizResponses.UpdateAsync(existingResponse);
        }
        else
        {
            // Create new response
            var quizResponse = new QuizResponse
            {
                StudentId = userId.Value,
                QuizId = quiz.Id,
                QuizQuestionId = questionId,
                QuizSessionId = session.Id,
                SelectedOption = answer,
                AnswerText = answerText,
                IsCorrect = isCorrect,
                PointsAwarded = pointsAwarded,
                AnsweredAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.QuizResponses.AddAsync(quizResponse);
        }

        // Update session points
        session = await _unitOfWork.QuizSessions.GetSessionWithResponsesAsync(session.Id);
        if (session != null)
        {
            session.PointsAwarded = session.Responses.Sum(r => r.PointsAwarded);
            await _unitOfWork.QuizSessions.UpdateAsync(session);
        }

        await _unitOfWork.SaveChangesAsync();

        // Broadcast progress to host (get assignment to find host)
        var assignment = await _unitOfWork.Assignments.GetByIdAsync(quiz.AssignmentId);
        if (assignment != null)
        {
            var progress = new
            {
                QuizId = quizId,
                StudentId = userId.Value,
                SessionId = session.Id,
                TotalQuestions = quiz.Questions.Count,
                AnsweredQuestions = session.Responses?.Count ?? 0,
                PointsAwarded = session.PointsAwarded,
                TotalPoints = session.TotalPoints,
                Progress = quiz.Questions.Count > 0 
                    ? (double)(session.Responses?.Count ?? 0) / quiz.Questions.Count * 100 
                    : 0
            };

            await Clients.User(assignment.CreatedById.ToString()).SendAsync("StudentProgress", progress);
        }

        // Confirm to student
        await Clients.Caller.SendAsync("AnswerSubmitted", new
        {
            QuizId = quizId,
            QuestionId = questionId,
            Answer = answer,
            IsCorrect = isCorrect,
            PointsAwarded = pointsAwarded
        });
    }

    /// <summary>
    /// Student submits quiz (completes QuizSession)
    /// </summary>
    public async Task SubmitQuiz(string quizId)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        // Find quiz
        var allQuizzes = await _unitOfWork.Quizzes.ListAsync();
        var quiz = allQuizzes.FirstOrDefault(q => q.QuizId == quizId);
        if (quiz == null)
        {
            throw new InvalidOperationException($"Quiz with ID {quizId} not found.");
        }

        // Get active session
        var session = await _unitOfWork.QuizSessions.GetActiveSessionByStudentAndQuizAsync(userId.Value, quiz.Id);
        if (session == null || session.IsCompleted)
        {
            throw new InvalidOperationException("Student has not started this quiz or quiz is already completed.");
        }

        // Load session with responses
        session = await _unitOfWork.QuizSessions.GetSessionWithResponsesAsync(session.Id);
        if (session == null)
        {
            throw new InvalidOperationException("Quiz session not found.");
        }

        // Calculate final scores
        session.EndedAt = DateTime.UtcNow;
        session.IsCompleted = true;
        session.PointsAwarded = session.Responses.Sum(r => r.PointsAwarded);
        session.TotalPoints = quiz.Questions.Sum(q => q.Points);

        // Calculate percentage score: (awardedPoints / totalPoints) * 100
        session.PercentageScore = session.TotalPoints > 0
            ? (decimal)(session.PointsAwarded / (double)session.TotalPoints * 100)
            : 0;

        // Get assignment for MaxScore
        var assignment = await _unitOfWork.Assignments.GetByIdAsync(quiz.AssignmentId);
        if (assignment == null)
        {
            throw new InvalidOperationException("Assignment not found.");
        }

        // Map percentage score to Score (0..MaxScore)
        session.Score = (int)(session.PercentageScore / 100 * assignment.MaxScore);

        // Check passing threshold
        if (quiz.PassingThreshold.HasValue)
        {
            session.Passed = session.PercentageScore >= quiz.PassingThreshold.Value;
        }

        // Create or update AssignmentSubmission
        var existingSubmission = await _unitOfWork.AssignmentSubmissions
            .GetSubmissionByAssignmentAndStudentAsync(quiz.AssignmentId, userId.Value);

        AssignmentSubmission submission;
        if (existingSubmission == null)
        {
            submission = new AssignmentSubmission
            {
                AssignmentId = quiz.AssignmentId,
                StudentId = userId.Value,
                AnswerText = JsonSerializer.Serialize(new
                {
                    QuizId = quiz.QuizId,
                    SessionId = session.Id,
                    Responses = session.Responses.Select(r => new
                    {
                        r.QuizQuestionId,
                        r.SelectedOption,
                        r.AnswerText,
                        r.IsCorrect,
                        r.PointsAwarded
                    }).ToList()
                }),
                SubmittedAt = DateTime.UtcNow,
                Score = session.Score,
                EvaluatedById = assignment.CreatedById, // Auto-evaluated
                EvaluatedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.AssignmentSubmissions.AddAsync(submission);
            await _unitOfWork.SaveChangesAsync(); // Save to get Submission.Id

            session.SubmissionId = submission.Id;
        }
        else
        {
            submission = existingSubmission;
            submission.AnswerText = JsonSerializer.Serialize(new
            {
                QuizId = quiz.QuizId,
                SessionId = session.Id,
                Responses = session.Responses.Select(r => new
                {
                    r.QuizQuestionId,
                    r.SelectedOption,
                    r.AnswerText,
                    r.IsCorrect,
                    r.PointsAwarded
                }).ToList()
            });
            submission.SubmittedAt = DateTime.UtcNow;
            submission.Score = session.Score;
            submission.EvaluatedById = assignment.CreatedById;
            submission.EvaluatedAt = DateTime.UtcNow;
            submission.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.AssignmentSubmissions.UpdateAsync(submission);
            session.SubmissionId = submission.Id;
        }

        await _unitOfWork.QuizSessions.UpdateAsync(session);
        await _unitOfWork.SaveChangesAsync();

        // Notify student
        await Clients.Caller.SendAsync("QuizResults", new
        {
            QuizId = quizId,
            SessionId = session.Id,
            PointsAwarded = session.PointsAwarded,
            TotalPoints = session.TotalPoints,
            PercentageScore = session.PercentageScore,
            Score = session.Score,
            MaxScore = assignment.MaxScore,
            Passed = session.Passed
        });
    }

    private int? GetUserId()
    {
        var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return null;
    }

    private List<object> GetQuestionsForStudent(Quiz quiz, QuizSession session)
    {
        var questions = quiz.Questions.OrderBy(q => q.Order).ToList();

        // Shuffle if enabled
        if (quiz.ShuffleQuestions)
        {
            var random = new Random((int)session.StartedAt.Ticks);
            questions = questions.OrderBy(x => random.Next()).ToList();
        }

        // Load existing responses to preserve order
        var responses = session.Responses?.ToList() ?? new List<QuizResponse>();

        return questions.Select(q => new
        {
            q.Id,
            q.Text,
            Options = JsonSerializer.Deserialize<List<string>>(q.Options) ?? new List<string>(),
            ExistingAnswer = responses.FirstOrDefault(r => r.QuizQuestionId == q.Id)?.SelectedOption
        }).ToList();
    }

    private bool EvaluateAnswer(string selectedAnswer, string correctAnswer)
    {
        // Support multiple correct answers (comma-separated)
        var correctAnswers = correctAnswer
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(a => a.Trim())
            .ToList();

        return correctAnswers.Any(ca => 
            ca.Equals(selectedAnswer, StringComparison.OrdinalIgnoreCase));
    }
}
