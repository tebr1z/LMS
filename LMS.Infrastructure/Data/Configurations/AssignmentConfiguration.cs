using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Data.Configurations;

public class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> builder)
    {
        builder.ToTable("Assignments");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.CoursePreparedId)
            .IsRequired(false); // Nullable: can be linked to CoursePrepared or Course

        builder.Property(a => a.CourseId)
            .IsRequired(false); // Nullable: can be linked to Course

        builder.Property(a => a.CourseInstanceId)
            .IsRequired(false); // Nullable: can be linked to CourseInstance

        builder.Property(a => a.GroupId)
            .IsRequired(false); // Nullable: can be course-wide or group-specific

        builder.Property(a => a.Title)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(a => a.Description)
            .HasMaxLength(2000);

        builder.Property(a => a.AssignmentType)
            .IsRequired()
            .HasConversion<int>(); // Store enum as int in database

        builder.Property(a => a.MaxScore)
            .IsRequired()
            .HasDefaultValue(100);

        builder.Property(a => a.CreatedById)
            .IsRequired();

        builder.Property(a => a.AllowEditAfterPublish)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(a => a.AllowResubmit)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(a => a.Deadline)
            .IsRequired(false); // Nullable deadline

        builder.Property(a => a.CreatedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(a => a.CoursePrepared)
            .WithMany(cp => cp.Assignments)
            .HasForeignKey(a => a.CoursePreparedId)
            .OnDelete(DeleteBehavior.SetNull); // Set null if CoursePrepared is deleted

        builder.HasOne(a => a.Course)
            .WithMany(c => c.Assignments)
            .HasForeignKey(a => a.CourseId)
            .OnDelete(DeleteBehavior.SetNull); // Set null if Course is deleted

        builder.HasOne(a => a.CourseInstance)
            .WithMany(ci => ci.Assignments)
            .HasForeignKey(a => a.CourseInstanceId)
            .OnDelete(DeleteBehavior.Cascade); // Cascade delete if CourseInstance is deleted

        builder.HasOne(a => a.Group)
            .WithMany()
            .HasForeignKey(a => a.GroupId)
            .OnDelete(DeleteBehavior.SetNull); // Set null if group is deleted

        // Check constraint: Assignment can be linked to CoursePrepared (template), Course, or CourseInstance (handled at application level)

        builder.HasMany(a => a.Submissions)
            .WithOne(s => s.Assignment)
            .HasForeignKey(s => s.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

