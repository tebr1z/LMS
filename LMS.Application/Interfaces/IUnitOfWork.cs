namespace LMS.Application.Interfaces;

public interface IUnitOfWork : IDisposable
{
    ICourseRepository Courses { get; }
    ICoursePreparedRepository CoursePrepareds { get; }
    ICourseInstanceRepository CourseInstances { get; }
    IEnrollmentRepository Enrollments { get; }
    IMessageRepository Messages { get; }
    IGroupRepository Groups { get; }
    ICourseGroupRepository CourseGroups { get; }
    IGroupUserRepository GroupUsers { get; }
    IAssignmentRepository Assignments { get; }
    IAssignmentSubmissionRepository AssignmentSubmissions { get; }
    IRepository<Domain.Entities.File> Files { get; }
    IQuizRepository Quizzes { get; }
    IQuizSessionRepository QuizSessions { get; }
    IRepository<Domain.Entities.QuizQuestion> QuizQuestions { get; }
    IRepository<Domain.Entities.QuizResponse> QuizResponses { get; }
    IAttendanceRepository Attendances { get; }
    IStudentNoteRepository StudentNotes { get; }
    IStudentFlagRepository StudentFlags { get; }
    IPaymentRepository Payments { get; }
    IInvoiceRepository Invoices { get; }
    IAssignmentTelemetryRepository AssignmentTelemetry { get; }
    IQuizTelemetryRepository QuizTelemetry { get; }
    ISystemSettingsRepository SystemSettings { get; }
    IStudentStatsRepository StudentStats { get; }
    INotificationRepository Notifications { get; }
    IAuditLogRepository AuditLogs { get; }
    ILearningLevelRepository LearningLevels { get; }
    IAchievementRepository Achievements { get; }
    IUserAchievementRepository UserAchievements { get; }
    IRewardPointRepository RewardPoints { get; }
    IRedeemableItemRepository RedeemableItems { get; }
    IAssignmentFeedbackAIRepository AssignmentFeedbackAI { get; }
    ILiveSessionRepository LiveSessions { get; }
    ICourseLocalizedRepository CourseLocalized { get; }
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}

