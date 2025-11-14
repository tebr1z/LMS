using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Data.Configurations;

public class EmailTemplateConfiguration : IEntityTypeConfiguration<EmailTemplate>
{
    public void Configure(EntityTypeBuilder<EmailTemplate> builder)
    {
        builder.ToTable("EmailTemplates");

        builder.HasKey(et => et.Id);

        builder.Property(et => et.TemplateType)
            .IsRequired()
            .HasMaxLength(100); // e.g., "NewAssignment", "DeadlineReminder", "GradePosted", etc.

        builder.Property(et => et.Subject)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(et => et.HtmlBody)
            .IsRequired()
            .HasColumnType("nvarchar(max)"); // HTML body can be large

        builder.Property(et => et.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(et => et.Description)
            .HasMaxLength(500);

        builder.Property(et => et.CreatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(et => et.TemplateType)
            .IsUnique()
            .HasDatabaseName("IX_EmailTemplates_TemplateType");

        builder.HasIndex(et => et.IsActive)
            .HasDatabaseName("IX_EmailTemplates_IsActive");
    }
}


