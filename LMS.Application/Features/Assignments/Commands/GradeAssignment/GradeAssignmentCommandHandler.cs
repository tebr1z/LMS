using LMS.Application.Interfaces;
using LMS.Application.Services;
using LMS.Domain.Enums;
using MediatR;

namespace LMS.Application.Features.Assignments.Commands.GradeAssignment;

public class GradeAssignmentCommandHandler : IRequestHandler<GradeAssignmentCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;
    private readonly ISettingsService _settingsService;

    public GradeAssignmentCommandHandler(IUnitOfWork unitOfWork, IUserRepository userRepository, ISettingsService settingsService)
    {
        _unitOfWork = unitOfWork;
        _userRepository = userRepository;
        _settingsService = settingsService;
    }

    public async Task<bool> Handle(GradeAssignmentCommand request, CancellationToken cancellationToken)
    {
        // Check if submission exists
        var submission = await _unitOfWork.AssignmentSubmissions.GetByIdAsync(request.SubmissionId);
        if (submission == null)
        {
            throw new InvalidOperationException($"Submission with ID {request.SubmissionId} not found.");
        }

        // Get assignment to validate score range and type
        var assignment = await _unitOfWork.Assignments.GetByIdAsync(submission.AssignmentId);
        if (assignment == null)
        {
            throw new InvalidOperationException($"Assignment with ID {submission.AssignmentId} not found.");
        }

        // Authorization check: Teacher/Admin only
        var evaluator = await _userRepository.GetUserByIdAsync(request.EvaluatedById);
        if (evaluator == null)
        {
            throw new UnauthorizedAccessException("Invalid user performing the action.");
        }

        if (evaluator.Role == UserRole.Mentor || evaluator.Role == UserRole.StudentOffice)
        {
            throw new UnauthorizedAccessException($"{evaluator.Role} cannot grade assignments. Only Teacher or Admin can grade assignments.");
        }
        else if (evaluator.Role != UserRole.Teacher && evaluator.Role != UserRole.MasterAdmin && evaluator.Role != UserRole.Admin)
        {
            throw new UnauthorizedAccessException("Only Teacher or Admin can grade assignments.");
        }

        // Business rule: ReadingMaterial type cannot be scored (MaxScore ignored) — mark Score null
        if (assignment.AssignmentType == AssignmentType.ReadingMaterial)
        {
            // For ReadingMaterial, set score to null regardless of input
            submission.Score = null;
            submission.Feedback = request.Feedback;
            submission.EvaluatedById = request.EvaluatedById;
            submission.EvaluatedAt = DateTime.UtcNow;
        }
        else
        {
            // Validate score range: Score (0..MaxScore)
            if (request.Score < 0 || request.Score > assignment.MaxScore)
            {
                throw new InvalidOperationException($"Score must be between 0 and {assignment.MaxScore}.");
            }

            submission.Score = request.Score;
            submission.Feedback = request.Feedback;
            submission.EvaluatedById = request.EvaluatedById;
            submission.EvaluatedAt = DateTime.UtcNow;

            // Compute percentage score: (Score / MaxScore) * 100
            submission.PercentageScore = assignment.MaxScore > 0 
                ? (decimal)(request.Score / (double)assignment.MaxScore * 100) 
                : 0;

            // Get pass thresholds from settings
            var assignmentPassPercent = await _settingsService.GetAssignmentPassPercentAsync();
            var highThreshold = await _settingsService.GetTeacherAssignmentHighThresholdAsync();

            // Determine if passed (percent >= AssignmentPassPercent)
            submission.Passed = submission.PercentageScore >= assignmentPassPercent;

            // Determine if excellent (percent >= TeacherAssignmentHighThreshold)
            submission.IsExcellent = submission.PercentageScore >= highThreshold;
        }

        submission.UpdatedAt = DateTime.UtcNow;

        await _unitOfWork.AssignmentSubmissions.UpdateAsync(submission);
        await _unitOfWork.SaveChangesAsync();

        // Update student stats
        await UpdateStudentStatsAsync(submission.StudentId, assignment.CourseInstanceId);

        return true;
    }

    private async Task UpdateStudentStatsAsync(int studentId, int? courseInstanceId)
    {
        // Get or create student stats for this course instance (or overall if null)
        var stats = await _unitOfWork.StudentStats.GetByStudentIdAsync(studentId, courseInstanceId);
        
        if (stats == null)
        {
            stats = new Domain.Entities.StudentStats
            {
                StudentId = studentId,
                CourseInstanceId = courseInstanceId,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.StudentStats.AddAsync(stats);
        }

        // Get all submissions for this student (filtered by course instance if specified)
        // Note: We need to load assignments separately to access AssignmentType
        var allSubmissions = await _unitOfWork.AssignmentSubmissions.ListAsync();
        var allAssignments = await _unitOfWork.Assignments.ListAsync();
        
        // Create a lookup for assignment types
        var assignmentTypeLookup = allAssignments.ToDictionary(a => a.Id, a => a.AssignmentType);
        
        var studentSubmissions = allSubmissions
            .Where(s => s.StudentId == studentId)
            .ToList();

        // Filter by course instance if specified
        if (courseInstanceId.HasValue)
        {
            var assignmentsInCourse = allAssignments
                .Where(a => a.CourseInstanceId == courseInstanceId.Value)
                .Select(a => a.Id)
                .ToHashSet();

            studentSubmissions = studentSubmissions
                .Where(s => assignmentsInCourse.Contains(s.AssignmentId))
                .ToList();
        }

        // Get all assignments for max score calculation
        var relevantAssignments = allAssignments
            .Where(a => studentSubmissions.Any(s => s.AssignmentId == a.Id))
            .ToList();

        // Calculate aggregated stats
        var scoredSubmissions = studentSubmissions
            .Where(s => s.Score.HasValue && assignmentTypeLookup.ContainsKey(s.AssignmentId) && assignmentTypeLookup[s.AssignmentId] != AssignmentType.ReadingMaterial)
            .ToList();
        
        stats.TotalPoints = scoredSubmissions.Sum(s => s.Score ?? 0);
        stats.TotalPossiblePoints = relevantAssignments
            .Where(a => a.AssignmentType != AssignmentType.ReadingMaterial && studentSubmissions.Any(s => s.AssignmentId == a.Id && s.Score.HasValue))
            .Sum(a => a.MaxScore);
        
        stats.AveragePercent = stats.TotalPossiblePoints > 0 
            ? (decimal)(stats.TotalPoints / (double)stats.TotalPossiblePoints * 100) 
            : (scoredSubmissions.Any() ? (decimal)scoredSubmissions.Average(s => (double)s.PercentageScore) : 0);
        
        stats.AssignmentsPassedCount = studentSubmissions.Count(s => s.Passed == true);
        stats.AssignmentsCompletedCount = studentSubmissions.Count(s => s.Score.HasValue || !string.IsNullOrEmpty(s.AnswerText) || !string.IsNullOrEmpty(s.FileUrl));

        // Get quiz stats
        var allQuizSessions = await _unitOfWork.QuizSessions.ListAsync();
        var studentQuizSessions = allQuizSessions
            .Where(qs => qs.StudentId == studentId)
            .ToList();

        if (courseInstanceId.HasValue)
        {
            var quizzesInCourse = (await _unitOfWork.Quizzes.ListAsync())
                .Where(q => relevantAssignments.Any(a => a.Id == q.AssignmentId))
                .Select(q => q.Id) // Quiz.Id is the foreign key in QuizSession
                .ToHashSet();

            studentQuizSessions = studentQuizSessions
                .Where(qs => quizzesInCourse.Contains(qs.QuizId))
                .ToList();
        }

        stats.QuizzesPassedCount = studentQuizSessions.Count(qs => qs.Passed == true);
        stats.QuizzesAttemptedCount = studentQuizSessions.Count;

        // Update last activity
        var lastSubmission = studentSubmissions.OrderByDescending(s => s.UpdatedAt ?? s.CreatedAt).FirstOrDefault();
        var lastQuiz = studentQuizSessions.OrderByDescending(qs => qs.UpdatedAt ?? qs.CreatedAt).FirstOrDefault();
        
        if (lastSubmission != null && lastQuiz != null)
        {
            stats.LastActivity = new[] { lastSubmission.UpdatedAt ?? lastSubmission.CreatedAt, lastQuiz.UpdatedAt ?? lastQuiz.CreatedAt }.Max();
        }
        else if (lastSubmission != null)
        {
            stats.LastActivity = lastSubmission.UpdatedAt ?? lastSubmission.CreatedAt;
        }
        else if (lastQuiz != null)
        {
            stats.LastActivity = lastQuiz.UpdatedAt ?? lastQuiz.CreatedAt;
        }

        stats.UpdatedAt = DateTime.UtcNow;

        if (stats.Id > 0)
        {
            await _unitOfWork.StudentStats.UpdateAsync(stats);
        }
        
        await _unitOfWork.SaveChangesAsync();
    }
}

