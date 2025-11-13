using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Data.Configurations;

public class QuizTelemetryConfiguration : IEntityTypeConfiguration<QuizTelemetry>
{
    public void Configure(EntityTypeBuilder<QuizTelemetry> builder)
    {
        builder.ToTable("QuizTelemetry");

        builder.HasKey(qt => qt.Id);

        builder.Property(qt => qt.StudentId)
            .IsRequired();

        builder.Property(qt => qt.QuizId)
            .IsRequired();

        builder.Property(qt => qt.QuizSessionId)
            .IsRequired(false);

        builder.Property(qt => qt.SecondsActive)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(qt => qt.SessionId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(qt => qt.Timestamp)
            .IsRequired();

        builder.Property(qt => qt.CreatedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(qt => qt.Quiz)
            .WithMany()
            .HasForeignKey(qt => qt.QuizId)
            .OnDelete(DeleteBehavior.NoAction); // No cascade to avoid multiple cascade paths (Quiz deletion handled via Assignment cascade)

        builder.HasOne(qt => qt.QuizSession)
            .WithMany()
            .HasForeignKey(qt => qt.QuizSessionId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes for faster lookups
        builder.HasIndex(qt => new { qt.StudentId, qt.QuizId, qt.SessionId })
            .HasDatabaseName("IX_QuizTelemetry_Student_Quiz_Session");

        builder.HasIndex(qt => qt.Timestamp)
            .HasDatabaseName("IX_QuizTelemetry_Timestamp");
    }
}

