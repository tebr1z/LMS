namespace LMS.Application.Interfaces;

public interface IUnitOfWork : IDisposable
{
    ICourseRepository Courses { get; }
    ICoursePreparedRepository CoursePrepareds { get; }
    IEnrollmentRepository Enrollments { get; }
    IMessageRepository Messages { get; }
    IGroupRepository Groups { get; }
    ICourseGroupRepository CourseGroups { get; }
    IGroupUserRepository GroupUsers { get; }
    IAssignmentRepository Assignments { get; }
    IAssignmentSubmissionRepository AssignmentSubmissions { get; }
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}

