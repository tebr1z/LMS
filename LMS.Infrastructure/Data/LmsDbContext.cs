using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using LMS.Domain.Entities;
using LMS.Infrastructure.Identity;

namespace LMS.Infrastructure.Data;

public class LmsDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, int>
{
    public LmsDbContext(DbContextOptions<LmsDbContext> options)
        : base(options)
    {
    }

    // DbSets
    public DbSet<Course> Courses { get; set; } = null!;
    public DbSet<CoursePrepared> CoursePrepareds { get; set; } = null!;
    public DbSet<CourseInstance> CourseInstances { get; set; } = null!;
    public DbSet<Enrollment> Enrollments { get; set; } = null!;
    public DbSet<Message> Messages { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
    public DbSet<Group> Groups { get; set; } = null!;
    public DbSet<CourseGroup> CourseGroups { get; set; } = null!;
    public DbSet<GroupUser> GroupUsers { get; set; } = null!;
    public DbSet<Assignment> Assignments { get; set; } = null!;
    public DbSet<AssignmentSubmission> AssignmentSubmissions { get; set; } = null!;
    public DbSet<File> Files { get; set; } = null!;
    public DbSet<Quiz> Quizzes { get; set; } = null!;
    public DbSet<QuizQuestion> QuizQuestions { get; set; } = null!;
    public DbSet<QuizResponse> QuizResponses { get; set; } = null!;
    public DbSet<QuizSession> QuizSessions { get; set; } = null!;
    public DbSet<Attendance> Attendances { get; set; } = null!;
    public DbSet<StudentNote> StudentNotes { get; set; } = null!;
    public DbSet<StudentFlag> StudentFlags { get; set; } = null!;
    public DbSet<Payment> Payments { get; set; } = null!;
    public DbSet<Invoice> Invoices { get; set; } = null!;
    public DbSet<AssignmentTelemetry> AssignmentTelemetry { get; set; } = null!;
    public DbSet<QuizTelemetry> QuizTelemetry { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply entity configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LmsDbContext).Assembly);
    }
}

