using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Data.Configurations;

public class StudentStatsConfiguration : IEntityTypeConfiguration<StudentStats>
{
    public void Configure(EntityTypeBuilder<StudentStats> builder)
    {
        builder.ToTable("StudentStats");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.StudentId)
            .IsRequired();

        builder.Property(s => s.CourseInstanceId)
            .IsRequired(false);

        builder.Property(s => s.TotalPoints)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(s => s.TotalPossiblePoints)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(s => s.AveragePercent)
            .IsRequired()
            .HasDefaultValue(0)
            .HasColumnType("decimal(5,2)"); // e.g., 85.50

        builder.Property(s => s.AssignmentsPassedCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(s => s.AssignmentsCompletedCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(s => s.QuizzesPassedCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(s => s.QuizzesAttemptedCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(s => s.LastActivity)
            .IsRequired(false);

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        // Unique constraint: one stats record per student per course instance (or overall if CourseInstanceId is null)
        builder.HasIndex(s => new { s.StudentId, s.CourseInstanceId })
            .IsUnique()
            .HasFilter("[CourseInstanceId] IS NOT NULL");

        // Index for leaderboard queries
        builder.HasIndex(s => new { s.CourseInstanceId, s.AveragePercent })
            .HasDatabaseName("IX_StudentStats_CourseInstance_AveragePercent");

        builder.HasIndex(s => s.StudentId)
            .HasDatabaseName("IX_StudentStats_Student");
    }
}

