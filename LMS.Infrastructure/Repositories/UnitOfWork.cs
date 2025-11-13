using LMS.Application.Interfaces;
using LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace LMS.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly LmsDbContext _context;
    private IDbContextTransaction? _transaction;
    private ICourseRepository? _courses;
    private ICoursePreparedRepository? _coursePrepareds;
    private ICourseInstanceRepository? _courseInstances;
    private IEnrollmentRepository? _enrollments;
    private IMessageRepository? _messages;
    private IGroupRepository? _groups;
    private ICourseGroupRepository? _courseGroups;
    private IGroupUserRepository? _groupUsers;
    private IAssignmentRepository? _assignments;
    private IAssignmentSubmissionRepository? _assignmentSubmissions;
    private IRepository<Domain.Entities.File>? _files;
    private IQuizRepository? _quizzes;
    private IQuizSessionRepository? _quizSessions;
    private IRepository<Domain.Entities.QuizQuestion>? _quizQuestions;
    private IRepository<Domain.Entities.QuizResponse>? _quizResponses;
    private IAttendanceRepository? _attendances;
    private IStudentNoteRepository? _studentNotes;
    private IStudentFlagRepository? _studentFlags;
    private IPaymentRepository? _payments;
    private IInvoiceRepository? _invoices;
    private IAssignmentTelemetryRepository? _assignmentTelemetry;
    private IQuizTelemetryRepository? _quizTelemetry;

    public UnitOfWork(LmsDbContext context)
    {
        _context = context;
    }

    public ICourseRepository Courses =>
        _courses ??= new CourseRepository(_context);

    public ICoursePreparedRepository CoursePrepareds =>
        _coursePrepareds ??= new CoursePreparedRepository(_context);

    public ICourseInstanceRepository CourseInstances =>
        _courseInstances ??= new CourseInstanceRepository(_context);

    public IEnrollmentRepository Enrollments =>
        _enrollments ??= new EnrollmentRepository(_context);

    public IMessageRepository Messages =>
        _messages ??= new MessageRepository(_context);

    public IGroupRepository Groups =>
        _groups ??= new GroupRepository(_context);

    public ICourseGroupRepository CourseGroups =>
        _courseGroups ??= new CourseGroupRepository(_context);

    public IGroupUserRepository GroupUsers =>
        _groupUsers ??= new GroupUserRepository(_context);

    public IAssignmentRepository Assignments =>
        _assignments ??= new AssignmentRepository(_context);

    public IAssignmentSubmissionRepository AssignmentSubmissions =>
        _assignmentSubmissions ??= new AssignmentSubmissionRepository(_context);

    public IRepository<Domain.Entities.File> Files =>
        _files ??= new EfRepository<Domain.Entities.File>(_context);

    public IQuizRepository Quizzes =>
        _quizzes ??= new QuizRepository(_context);

    public IQuizSessionRepository QuizSessions =>
        _quizSessions ??= new QuizSessionRepository(_context);

    public IRepository<Domain.Entities.QuizQuestion> QuizQuestions =>
        _quizQuestions ??= new EfRepository<Domain.Entities.QuizQuestion>(_context);

    public IRepository<Domain.Entities.QuizResponse> QuizResponses =>
        _quizResponses ??= new EfRepository<Domain.Entities.QuizResponse>(_context);

    public IAttendanceRepository Attendances =>
        _attendances ??= new AttendanceRepository(_context);

    public IStudentNoteRepository StudentNotes =>
        _studentNotes ??= new StudentNoteRepository(_context);

    public IStudentFlagRepository StudentFlags =>
        _studentFlags ??= new StudentFlagRepository(_context);

    public IPaymentRepository Payments =>
        _payments ??= new PaymentRepository(_context);

    public IInvoiceRepository Invoices =>
        _invoices ??= new InvoiceRepository(_context);

    public IAssignmentTelemetryRepository AssignmentTelemetry =>
        _assignmentTelemetry ??= new AssignmentTelemetryRepository(_context);

    public IQuizTelemetryRepository QuizTelemetry =>
        _quizTelemetry ??= new QuizTelemetryRepository(_context);

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}

