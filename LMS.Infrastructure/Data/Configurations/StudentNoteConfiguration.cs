using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Data.Configurations;

public class StudentNoteConfiguration : IEntityTypeConfiguration<StudentNote>
{
    public void Configure(EntityTypeBuilder<StudentNote> builder)
    {
        builder.ToTable("StudentNotes");

        builder.HasKey(sn => sn.Id);

        builder.Property(sn => sn.StudentId)
            .IsRequired();

        builder.Property(sn => sn.GroupId)
            .IsRequired();

        builder.Property(sn => sn.CreatedById)
            .IsRequired();

        builder.Property(sn => sn.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(sn => sn.Content)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(sn => sn.IsPrivate)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(sn => sn.IsImportant)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(sn => sn.CreatedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(sn => sn.Group)
            .WithMany()
            .HasForeignKey(sn => sn.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index for faster lookups
        builder.HasIndex(sn => new { sn.StudentId, sn.GroupId })
            .HasDatabaseName("IX_StudentNotes_Student_Group");

        builder.HasIndex(sn => new { sn.GroupId, sn.CreatedAt })
            .HasDatabaseName("IX_StudentNotes_Group_CreatedAt");
    }
}

