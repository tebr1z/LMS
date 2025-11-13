using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Data.Configurations;

public class AttendanceConfiguration : IEntityTypeConfiguration<Attendance>
{
    public void Configure(EntityTypeBuilder<Attendance> builder)
    {
        builder.ToTable("Attendances");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.GroupId)
            .IsRequired();

        builder.Property(a => a.StudentId)
            .IsRequired();

        builder.Property(a => a.Date)
            .IsRequired();

        builder.Property(a => a.Present)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(a => a.MarkedById)
            .IsRequired();

        builder.Property(a => a.Notes)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(a => a.CreatedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(a => a.Group)
            .WithMany()
            .HasForeignKey(a => a.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint: one attendance record per student per date per group
        builder.HasIndex(a => new { a.GroupId, a.StudentId, a.Date })
            .IsUnique();

        // Index for faster lookups
        builder.HasIndex(a => new { a.GroupId, a.Date })
            .HasDatabaseName("IX_Attendances_Group_Date");
    }
}

