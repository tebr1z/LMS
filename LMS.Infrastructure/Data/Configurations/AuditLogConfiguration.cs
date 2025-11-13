using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Entity)
            .IsRequired()
            .HasMaxLength(100); // e.g., "Assignment", "Submission", "CourseInstance"

        builder.Property(a => a.EntityId)
            .IsRequired();

        builder.Property(a => a.Action)
            .IsRequired()
            .HasMaxLength(50); // e.g., "Create", "Update", "Delete", "Grade"

        builder.Property(a => a.UserId)
            .IsRequired();

        builder.Property(a => a.OldValue)
            .HasMaxLength(5000) // JSON string of old values
            .IsRequired(false);

        builder.Property(a => a.NewValue)
            .HasMaxLength(5000) // JSON string of new values
            .IsRequired(false);

        builder.Property(a => a.Timestamp)
            .IsRequired();

        builder.Property(a => a.Description)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(a => a.CreatedAt)
            .IsRequired();

        // Indexes for faster lookups
        builder.HasIndex(a => new { a.Entity, a.EntityId })
            .HasDatabaseName("IX_AuditLogs_Entity_EntityId");

        builder.HasIndex(a => a.UserId)
            .HasDatabaseName("IX_AuditLogs_UserId");

        builder.HasIndex(a => a.Action)
            .HasDatabaseName("IX_AuditLogs_Action");

        builder.HasIndex(a => a.Timestamp)
            .HasDatabaseName("IX_AuditLogs_Timestamp");
    }
}


