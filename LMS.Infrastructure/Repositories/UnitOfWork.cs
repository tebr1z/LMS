using LMS.Application.Interfaces;
using LMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;

namespace LMS.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly LmsDbContext _context;
    private IDbContextTransaction? _transaction;
    private ICourseRepository? _courses;
    private IEnrollmentRepository? _enrollments;
    private IMessageRepository? _messages;
    private IGroupRepository? _groups;
    private ICourseGroupRepository? _courseGroups;
    private IGroupUserRepository? _groupUsers;
    private IAssignmentRepository? _assignments;
    private IAssignmentSubmissionRepository? _assignmentSubmissions;

    public UnitOfWork(LmsDbContext context)
    {
        _context = context;
    }

    public ICourseRepository Courses =>
        _courses ??= new CourseRepository(_context);

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

