using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Data.Configurations;

public class LearningLevelConfiguration : IEntityTypeConfiguration<LearningLevel>
{
    public void Configure(EntityTypeBuilder<LearningLevel> builder)
    {
        builder.ToTable("LearningLevels");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.StudentId)
            .IsRequired();

        builder.Property(l => l.DifficultyLevel)
            .IsRequired()
            .HasDefaultValue(2); // Default: Medium

        // Note: UpdatedAt and CreatedAt are inherited from BaseEntity
        builder.Property(l => l.CreatedAt)
            .IsRequired();

        // Unique constraint: one learning level per student
        builder.HasIndex(l => l.StudentId)
            .IsUnique()
            .HasDatabaseName("IX_LearningLevels_StudentId");

        // Index for faster lookups
        builder.HasIndex(l => l.DifficultyLevel)
            .HasDatabaseName("IX_LearningLevels_DifficultyLevel");
    }
}

