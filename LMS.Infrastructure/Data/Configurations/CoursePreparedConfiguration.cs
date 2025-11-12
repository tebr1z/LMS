using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Data.Configurations;

public class CoursePreparedConfiguration : IEntityTypeConfiguration<CoursePrepared>
{
    public void Configure(EntityTypeBuilder<CoursePrepared> builder)
    {
        builder.ToTable("CoursePrepareds");

        builder.HasKey(cp => cp.Id);

        builder.Property(cp => cp.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(cp => cp.Description)
            .HasMaxLength(2000);

        builder.Property(cp => cp.DefaultContent)
            .HasColumnType("nvarchar(max)");

        builder.Property(cp => cp.CreatedAt)
            .IsRequired();

        // Relationships
        builder.HasMany(cp => cp.CourseGroups)
            .WithOne(cg => cg.CoursePrepared)
            .HasForeignKey(cg => cg.CoursePreparedId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(cp => cp.Assignments)
            .WithOne(a => a.CoursePrepared)
            .HasForeignKey(a => a.CoursePreparedId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

