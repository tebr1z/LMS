using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Data.Configurations;

public class CourseInstanceConfiguration : IEntityTypeConfiguration<CourseInstance>
{
    public void Configure(EntityTypeBuilder<CourseInstance> builder)
    {
        builder.ToTable("CourseInstances");

        builder.HasKey(ci => ci.Id);

        builder.Property(ci => ci.CoursePreparedId)
            .IsRequired();

        builder.Property(ci => ci.GroupId)
            .IsRequired();

        builder.Property(ci => ci.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(ci => ci.Description)
            .HasMaxLength(2000);

        builder.Property(ci => ci.Content)
            .HasColumnType("nvarchar(max)");

        builder.Property(ci => ci.CreatedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(ci => ci.CoursePrepared)
            .WithMany()
            .HasForeignKey(ci => ci.CoursePreparedId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ci => ci.Group)
            .WithMany()
            .HasForeignKey(ci => ci.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        // Note: Assignments relationship is configured in AssignmentConfiguration

        // Unique constraint: one CourseInstance per CoursePrepared per Group
        builder.HasIndex(ci => new { ci.CoursePreparedId, ci.GroupId })
            .IsUnique();
    }
}

