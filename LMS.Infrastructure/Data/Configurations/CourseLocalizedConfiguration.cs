using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Data.Configurations;

public class CourseLocalizedConfiguration : IEntityTypeConfiguration<CourseLocalized>
{
    public void Configure(EntityTypeBuilder<CourseLocalized> builder)
    {
        builder.ToTable("CourseLocalized");

        builder.HasKey(cl => cl.Id);

        builder.Property(cl => cl.CourseId)
            .IsRequired();

        builder.Property(cl => cl.LangCode)
            .IsRequired()
            .HasMaxLength(10); // e.g., "en", "tr", "az", "ru"

        builder.Property(cl => cl.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(cl => cl.Description)
            .HasMaxLength(2000);

        builder.Property(cl => cl.ContentUrlLocalized)
            .HasMaxLength(500);

        // Relationships
        builder.HasOne(cl => cl.Course)
            .WithMany()
            .HasForeignKey(cl => cl.CourseId)
            .OnDelete(DeleteBehavior.Cascade); // Delete localized content when course is deleted

        // Unique constraint: one translation per course per language
        builder.HasIndex(cl => new { cl.CourseId, cl.LangCode })
            .IsUnique()
            .HasDatabaseName("IX_CourseLocalized_CourseId_LangCode");

        // Indexes for faster lookups
        builder.HasIndex(cl => cl.CourseId)
            .HasDatabaseName("IX_CourseLocalized_CourseId");

        builder.HasIndex(cl => cl.LangCode)
            .HasDatabaseName("IX_CourseLocalized_LangCode");
    }
}

