using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Data.Configurations;

public class CourseGroupConfiguration : IEntityTypeConfiguration<CourseGroup>
{
    public void Configure(EntityTypeBuilder<CourseGroup> builder)
    {
        builder.ToTable("CourseGroups");

        builder.HasKey(cg => cg.Id);

        builder.Property(cg => cg.CourseId)
            .IsRequired();

        builder.Property(cg => cg.GroupId)
            .IsRequired();

        builder.Property(cg => cg.CreatedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(cg => cg.Course)
            .WithMany(c => c.CourseGroups)
            .HasForeignKey(cg => cg.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(cg => cg.Group)
            .WithMany(g => g.CourseGroups)
            .HasForeignKey(cg => cg.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint: A group can only be assigned to a course once
        builder.HasIndex(cg => new { cg.CourseId, cg.GroupId })
            .IsUnique();
    }
}

