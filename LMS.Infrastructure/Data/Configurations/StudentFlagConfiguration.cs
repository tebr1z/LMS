using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Data.Configurations;

public class StudentFlagConfiguration : IEntityTypeConfiguration<StudentFlag>
{
    public void Configure(EntityTypeBuilder<StudentFlag> builder)
    {
        builder.ToTable("StudentFlags");

        builder.HasKey(sf => sf.Id);

        builder.Property(sf => sf.StudentId)
            .IsRequired();

        builder.Property(sf => sf.CreatedById)
            .IsRequired();

        builder.Property(sf => sf.Reason)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(sf => sf.RecommendedAction)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(sf => sf.IsResolved)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(sf => sf.ResolvedAt)
            .IsRequired(false);

        builder.Property(sf => sf.ResolvedById)
            .IsRequired(false);

        builder.Property(sf => sf.ResolutionNotes)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(sf => sf.CreatedAt)
            .IsRequired();

        // Index for faster lookups
        builder.HasIndex(sf => new { sf.StudentId, sf.IsResolved })
            .HasDatabaseName("IX_StudentFlags_Student_Resolved");

        builder.HasIndex(sf => sf.CreatedById)
            .HasDatabaseName("IX_StudentFlags_CreatedBy");
    }
}

