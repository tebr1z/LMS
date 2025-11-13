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
    
    // Track active quizzes: quizId -> QuizData
    private static readonly Dictionary<string, QuizData> _activeQuizzes = new();
    
    // Track student answers: (quizId, studentId) -> StudentQuizData
    private static readonly Dictionary<(string quizId, int studentId), StudentQuizData> _studentAnswers = new();

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
    /// Host (teacher) starts quiz for CourseInstance or AssignmentTestId
    /// Server sends "QuizStarted" with quizId, questions
    /// </summary>
    public async Task StartQuiz(int? courseInstanceId = null, int? assignmentTestId = null)
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

        Assignment? assignment = null;

        if (assignmentTestId.HasValue)
        {
            // Quiz is for a specific Assignment (Test type)
            assignment = await _unitOfWork.Assignments.GetByIdAsync(assignmentTestId.Value);
            if (assignment == null)
            {
                throw new InvalidOperationException($"Assignment with ID {assignmentTestId.Value} not found.");
            }

            if (assignment.AssignmentType != AssignmentType.Test)
            {
                throw new InvalidOperationException("Assignment must be of type Test to start a quiz.");
            }
        }
        else if (courseInstanceId.HasValue)
        {
            // Quiz is for a CourseInstance (find Test assignment for that CourseInstance)
            var courseInstance = await _unitOfWork.CourseInstances.GetByIdAsync(courseInstanceId.Value);
            if (courseInstance == null)
            {
                throw new InvalidOperationException($"CourseInstance with ID {courseInstanceId.Value} not found.");
            }

            // Find Test assignments for this CourseInstance
            var allAssignments = await _unitOfWork.Assignments.ListAsync();
            var testAssignments = allAssignments
                .Where(a => a.CourseInstanceId == courseInstanceId.Value && a.AssignmentType == AssignmentType.Test)
                .ToList();

            if (testAssignments.Count == 0)
            {
                throw new InvalidOperationException($"No Test assignment found for CourseInstance with ID {courseInstanceId.Value}. Please create a Test assignment first.");
            }

            // Use the first Test assignment found (or most recent)
            assignment = testAssignments.OrderByDescending(a => a.CreatedAt).First();
        }
        else
        {
            throw new InvalidOperationException("Either courseInstanceId or assignmentTestId must be provided.");
        }

        // Generate unique quiz ID
        var quizId = Guid.NewGuid().ToString();

        // Parse questions from assignment description (assuming JSON format)
        // Format: {"questions": [{"id": 1, "text": "...", "options": [...], "correctAnswer": ...}, ...]}
        var questions = ParseQuestionsFromAssignment(assignment);

        // Store quiz data
        var quizData = new QuizData
        {
            QuizId = quizId,
            AssignmentId = assignment.Id,
            CourseInstanceId = assignment.CourseInstanceId,
            GroupId = assignment.GroupId,
            HostId = userId.Value,
            Questions = questions,
            StartedAt = DateTime.UtcNow
        };

        _activeQuizzes[quizId] = quizData;

        // Send "QuizStarted" to all students in the group/course
        var groupName = assignment.GroupId.HasValue ? $"group_{assignment.GroupId}" : $"quiz_{quizId}";
        await Clients.Group(groupName).SendAsync("QuizStarted", new
        {
            QuizId = quizId,
            AssignmentId = assignment.Id,
            Questions = questions.Select(q => new
            {
                q.Id,
                q.Text,
                q.Options,
                // Don't send correct answer to students
            }).ToList()
        });

        await Clients.Caller.SendAsync("QuizHostStarted", new
        {
            QuizId = quizId,
            AssignmentId = assignment.Id,
            Questions = questions
        });
    }

    /// <summary>
    /// Students join using JoinQuiz(quizId)
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

        // Check if quiz exists
        if (!_activeQuizzes.ContainsKey(quizId))
        {
            throw new InvalidOperationException($"Quiz with ID {quizId} not found or has ended.");
        }

        var quizData = _activeQuizzes[quizId];

        // Check if student belongs to the group (if quiz is group-specific)
        if (quizData.GroupId.HasValue)
        {
            var isUserInGroup = await _unitOfWork.Groups.IsUserInGroupAsync(quizData.GroupId.Value, userId.Value);
            if (!isUserInGroup)
            {
                throw new UnauthorizedAccessException("Student is not a member of the group for this quiz.");
            }
        }

        // Initialize student quiz data
        if (!_studentAnswers.ContainsKey((quizId, userId.Value)))
        {
            _studentAnswers[(quizId, userId.Value)] = new StudentQuizData
            {
                QuizId = quizId,
                StudentId = userId.Value,
                Answers = new Dictionary<int, string>(),
                StartedAt = DateTime.UtcNow
            };
        }

        // Add student to quiz group
        var quizGroupName = $"quiz_{quizId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, quizGroupName);

        await Clients.Caller.SendAsync("JoinedQuiz", new
        {
            QuizId = quizId,
            AssignmentId = quizData.AssignmentId,
            Questions = quizData.Questions.Select(q => new
            {
                q.Id,
                q.Text,
                q.Options
            }).ToList()
        });

        // Notify host
        await Clients.User(quizData.HostId.ToString()).SendAsync("StudentJoinedQuiz", new
        {
            QuizId = quizId,
            StudentId = userId.Value
        });
    }

    /// <summary>
    /// StudentAnswer(quizId, questionId, answer) -> server can process auto-score for Test-type and broadcast current progress
    /// </summary>
    public async Task StudentAnswer(string quizId, int questionId, string answer)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        // Check if quiz exists
        if (!_activeQuizzes.ContainsKey(quizId))
        {
            throw new InvalidOperationException($"Quiz with ID {quizId} not found or has ended.");
        }

        var quizData = _activeQuizzes[quizId];

        // Check if student has joined the quiz
        if (!_studentAnswers.ContainsKey((quizId, userId.Value)))
        {
            throw new InvalidOperationException("Student has not joined this quiz.");
        }

        var studentData = _studentAnswers[(quizId, userId.Value)];

        // Store answer
        studentData.Answers[questionId] = answer;

        // Auto-score for Test-type (compare with correct answer)
        var question = quizData.Questions.FirstOrDefault(q => q.Id == questionId);
        if (question != null && question.CorrectAnswer.Equals(answer, StringComparison.OrdinalIgnoreCase))
        {
            studentData.CorrectAnswersCount++;
        }

        // Broadcast current progress to host
        var progress = new
        {
            QuizId = quizId,
            StudentId = userId.Value,
            TotalQuestions = quizData.Questions.Count,
            AnsweredQuestions = studentData.Answers.Count,
            CorrectAnswers = studentData.CorrectAnswersCount,
            Progress = (double)studentData.Answers.Count / quizData.Questions.Count * 100
        };

        await Clients.User(quizData.HostId.ToString()).SendAsync("StudentProgress", progress);

        // Confirm to student
        await Clients.Caller.SendAsync("AnswerSubmitted", new
        {
            QuizId = quizId,
            QuestionId = questionId,
            Answer = answer,
            IsCorrect = question != null && question.CorrectAnswer.Equals(answer, StringComparison.OrdinalIgnoreCase)
        });
    }

    /// <summary>
    /// QuizEnd -> server calculates scores, persists results in AssignmentSubmission for that student (auto create submission)
    /// </summary>
    public async Task QuizEnd(string quizId)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
        {
            throw new UnauthorizedAccessException("User is not authenticated.");
        }

        // Check if quiz exists
        if (!_activeQuizzes.ContainsKey(quizId))
        {
            throw new InvalidOperationException($"Quiz with ID {quizId} not found.");
        }

        var quizData = _activeQuizzes[quizId];

        // Only host can end quiz
        if (quizData.HostId != userId.Value)
        {
            throw new UnauthorizedAccessException("Only the quiz host can end the quiz.");
        }

        // Calculate scores for all students and persist results
        var assignment = await _unitOfWork.Assignments.GetByIdAsync(quizData.AssignmentId);
        if (assignment == null)
        {
            throw new InvalidOperationException($"Assignment with ID {quizData.AssignmentId} not found.");
        }

        var studentResults = new List<object>();

        foreach (var (key, studentData) in _studentAnswers.Where(kvp => kvp.Key.quizId == quizId))
        {
            var studentId = key.studentId;

            // Calculate final score
            var totalQuestions = quizData.Questions.Count;
            var correctAnswers = 0;

            foreach (var (qId, answer) in studentData.Answers)
            {
                var question = quizData.Questions.FirstOrDefault(q => q.Id == qId);
                if (question != null && question.CorrectAnswer.Equals(answer, StringComparison.OrdinalIgnoreCase))
                {
                    correctAnswers++;
                }
            }

            // Calculate score as percentage of MaxScore
            var scorePercentage = totalQuestions > 0 ? (double)correctAnswers / totalQuestions : 0;
            var finalScore = (int)(scorePercentage * assignment.MaxScore);

            // Auto-create AssignmentSubmission for that student
            var existingSubmission = await _unitOfWork.AssignmentSubmissions
                .GetSubmissionByAssignmentAndStudentAsync(quizData.AssignmentId, studentId);

            if (existingSubmission == null)
            {
                // Create new submission
                var submission = new AssignmentSubmission
                {
                    AssignmentId = quizData.AssignmentId,
                    StudentId = studentId,
                    AnswerText = JsonSerializer.Serialize(new
                    {
                        QuizId = quizId,
                        Answers = studentData.Answers,
                        CorrectAnswers = correctAnswers,
                        TotalQuestions = totalQuestions
                    }),
                    SubmittedAt = DateTime.UtcNow,
                    Score = finalScore, // Auto-score for Test-type
                    EvaluatedById = quizData.HostId, // Auto-evaluated by system
                    EvaluatedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.AssignmentSubmissions.AddAsync(submission);
            }
            else
            {
                // Update existing submission
                existingSubmission.AnswerText = JsonSerializer.Serialize(new
                {
                    QuizId = quizId,
                    Answers = studentData.Answers,
                    CorrectAnswers = correctAnswers,
                    TotalQuestions = totalQuestions
                });
                existingSubmission.SubmittedAt = DateTime.UtcNow;
                existingSubmission.Score = finalScore;
                existingSubmission.EvaluatedById = quizData.HostId;
                existingSubmission.EvaluatedAt = DateTime.UtcNow;
                existingSubmission.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.AssignmentSubmissions.UpdateAsync(existingSubmission);
            }

            studentResults.Add(new
            {
                StudentId = studentId,
                CorrectAnswers = correctAnswers,
                TotalQuestions = totalQuestions,
                Score = finalScore,
                MaxScore = assignment.MaxScore
            });

            // Notify student
            await Clients.User(studentId.ToString()).SendAsync("QuizResults", new
            {
                QuizId = quizId,
                CorrectAnswers = correctAnswers,
                TotalQuestions = totalQuestions,
                Score = finalScore,
                MaxScore = assignment.MaxScore
            });
        }

        await _unitOfWork.SaveChangesAsync();

        // Broadcast quiz ended
        var quizGroupName = $"quiz_{quizId}";
        await Clients.Group(quizGroupName).SendAsync("QuizEnded", new
        {
            QuizId = quizId,
            Results = studentResults
        });

        // Clean up
        _activeQuizzes.Remove(quizId);
        var keysToRemove = _studentAnswers.Keys.Where(k => k.quizId == quizId).ToList();
        foreach (var key in keysToRemove)
        {
            _studentAnswers.Remove(key);
        }

        await Clients.Caller.SendAsync("QuizEndedByHost", new { QuizId = quizId });
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

    private List<QuizQuestion> ParseQuestionsFromAssignment(Assignment assignment)
    {
        // Parse questions from assignment description (assumes JSON format)
        // For now, return empty list if description doesn't contain valid JSON
        // In production, you might have a separate Question entity
        
        if (string.IsNullOrWhiteSpace(assignment.Description))
        {
            return new List<QuizQuestion>();
        }

        try
        {
            var questionsJson = JsonSerializer.Deserialize<JsonElement>(assignment.Description);
            if (questionsJson.TryGetProperty("questions", out var questionsElement))
            {
                var questions = new List<QuizQuestion>();
                foreach (var q in questionsElement.EnumerateArray())
                {
                    questions.Add(new QuizQuestion
                    {
                        Id = q.GetProperty("id").GetInt32(),
                        Text = q.GetProperty("text").GetString() ?? "",
                        Options = q.TryGetProperty("options", out var options) 
                            ? options.EnumerateArray().Select(o => o.GetString() ?? "").ToList()
                            : new List<string>(),
                        CorrectAnswer = q.GetProperty("correctAnswer").GetString() ?? ""
                    });
                }
                return questions;
            }
        }
        catch
        {
            // Invalid JSON, return empty
        }

        return new List<QuizQuestion>();
    }

    // Helper classes for quiz data
    private class QuizData
    {
        public string QuizId { get; set; } = string.Empty;
        public int AssignmentId { get; set; }
        public int? CourseInstanceId { get; set; }
        public int? GroupId { get; set; }
        public int HostId { get; set; }
        public List<QuizQuestion> Questions { get; set; } = new();
        public DateTime StartedAt { get; set; }
    }

    private class StudentQuizData
    {
        public string QuizId { get; set; } = string.Empty;
        public int StudentId { get; set; }
        public Dictionary<int, string> Answers { get; set; } = new();
        public int CorrectAnswersCount { get; set; }
        public DateTime StartedAt { get; set; }
    }

    private class QuizQuestion
    {
        public int Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new();
        public string CorrectAnswer { get; set; } = string.Empty;
    }
}

