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
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}

