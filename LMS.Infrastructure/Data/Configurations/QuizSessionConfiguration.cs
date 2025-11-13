using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Data.Configurations;

public class QuizSessionConfiguration : IEntityTypeConfiguration<QuizSession>
{
    public void Configure(EntityTypeBuilder<QuizSession> builder)
    {
        builder.ToTable("QuizSessions");

        builder.HasKey(qs => qs.Id);

        builder.Property(qs => qs.StudentId)
            .IsRequired();

        builder.Property(qs => qs.QuizId)
            .IsRequired();

        builder.Property(qs => qs.AssignmentId)
            .IsRequired();

        builder.Property(qs => qs.StartedAt)
            .IsRequired();

        builder.Property(qs => qs.EndedAt)
            .IsRequired(false);

        builder.Property(qs => qs.ExpiresAt)
            .IsRequired(false);

        builder.Property(qs => qs.IsCompleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(qs => qs.TotalPoints)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(qs => qs.PointsAwarded)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(qs => qs.Score)
            .IsRequired(false);

        builder.Property(qs => qs.PercentageScore)
            .IsRequired()
            .HasDefaultValue(0)
            .HasColumnType("decimal(5,2)"); // e.g., 85.50

        builder.Property(qs => qs.Passed)
            .IsRequired(false);

        builder.Property(qs => qs.SubmissionId)
            .IsRequired(false);

        builder.Property(qs => qs.CreatedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(qs => qs.Quiz)
            .WithMany(q => q.Sessions)
            .HasForeignKey(qs => qs.QuizId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(qs => qs.Assignment)
            .WithMany()
            .HasForeignKey(qs => qs.AssignmentId)
            .OnDelete(DeleteBehavior.Restrict); // Don't delete if assignment is deleted (for history)

        builder.HasOne(qs => qs.Submission)
            .WithMany(s => s.QuizSessions)
            .HasForeignKey(qs => qs.SubmissionId)
            .OnDelete(DeleteBehavior.SetNull); // Set null if submission is deleted

        builder.HasMany(qs => qs.Responses)
            .WithOne(qr => qr.Session)
            .HasForeignKey(qr => qr.QuizSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index for faster lookups
        builder.HasIndex(qs => new { qs.StudentId, qs.QuizId, qs.IsCompleted })
            .HasDatabaseName("IX_QuizSessions_Student_Quiz_Completed");
    }
}

