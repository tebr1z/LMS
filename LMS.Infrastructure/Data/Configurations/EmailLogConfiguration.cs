using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Data.Configurations;

public class EmailLogConfiguration : IEntityTypeConfiguration<EmailLog>
{
    public void Configure(EntityTypeBuilder<EmailLog> builder)
    {
        builder.ToTable("EmailLogs");

        builder.HasKey(el => el.Id);

        builder.Property(el => el.To)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(el => el.Subject)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(el => el.Body)
            .HasColumnType("nvarchar(max)"); // Body can be large

        builder.Property(el => el.SentAt)
            .IsRequired();

        builder.Property(el => el.Status)
            .IsRequired()
            .HasMaxLength(50); // "Sent", "Failed", "Pending"

        builder.Property(el => el.Error)
            .HasMaxLength(1000);

        builder.Property(el => el.TemplateType)
            .HasMaxLength(100);

        builder.Property(el => el.RelatedEntityType)
            .HasMaxLength(100);

        builder.Property(el => el.CreatedAt)
            .IsRequired();

        // Indexes for faster lookups
        builder.HasIndex(el => el.To)
            .HasDatabaseName("IX_EmailLogs_To");

        builder.HasIndex(el => el.Status)
            .HasDatabaseName("IX_EmailLogs_Status");

        builder.HasIndex(el => el.SentAt)
            .HasDatabaseName("IX_EmailLogs_SentAt");

        builder.HasIndex(el => el.UserId)
            .HasDatabaseName("IX_EmailLogs_UserId");

        builder.HasIndex(el => el.TemplateType)
            .HasDatabaseName("IX_EmailLogs_TemplateType");
    }
}


