using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Data.Configurations;

public class AssignmentTelemetryConfiguration : IEntityTypeConfiguration<AssignmentTelemetry>
{
    public void Configure(EntityTypeBuilder<AssignmentTelemetry> builder)
    {
        builder.ToTable("AssignmentTelemetry");

        builder.HasKey(at => at.Id);

        builder.Property(at => at.StudentId)
            .IsRequired();

        builder.Property(at => at.AssignmentId)
            .IsRequired();

        builder.Property(at => at.SecondsActive)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(at => at.SessionId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(at => at.Timestamp)
            .IsRequired();

        builder.Property(at => at.SubmissionId)
            .IsRequired(false);

        builder.Property(at => at.CreatedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(at => at.Assignment)
            .WithMany()
            .HasForeignKey(at => at.AssignmentId)
            .OnDelete(DeleteBehavior.NoAction); // No cascade to avoid multiple cascade paths (Assignment deletion handled via Submission cascade)

        builder.HasOne(at => at.Submission)
            .WithMany()
            .HasForeignKey(at => at.SubmissionId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes for faster lookups and aggregation
        builder.HasIndex(at => new { at.StudentId, at.AssignmentId, at.SessionId })
            .HasDatabaseName("IX_AssignmentTelemetry_Student_Assignment_Session");

        builder.HasIndex(at => at.Timestamp)
            .HasDatabaseName("IX_AssignmentTelemetry_Timestamp");
    }
}

