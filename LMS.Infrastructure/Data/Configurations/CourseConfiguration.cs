using LMS.Domain.Entities;
using LMS.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Data.Configurations;

public class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.ToTable("Courses");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(c => c.Description)
            .HasMaxLength(2000);

        builder.Property(c => c.CreatedBy)
            .IsRequired();

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        // Navigation properties
        builder.HasMany(c => c.Enrollments)
            .WithOne(e => e.Course)
            .HasForeignKey(e => e.CourseId)
            .OnDelete(DeleteBehavior.Cascade); // Delete enrollments when course is deleted

        builder.HasMany(c => c.CourseGroups)
            .WithOne(cg => cg.Course)
            .HasForeignKey(cg => cg.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(c => c.Assignments)
            .WithOne(a => a.Course)
            .HasForeignKey(a => a.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relationship with ApplicationUser
        builder.HasOne<ApplicationUser>()
            .WithMany(u => u.CreatedCourses)
            .HasForeignKey(c => c.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

