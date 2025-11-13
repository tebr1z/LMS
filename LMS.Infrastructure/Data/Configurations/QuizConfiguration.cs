using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Data.Configurations;

public class QuizConfiguration : IEntityTypeConfiguration<Quiz>
{
    public void Configure(EntityTypeBuilder<Quiz> builder)
    {
        builder.ToTable("Quizzes");

        builder.HasKey(q => q.Id);

        builder.Property(q => q.AssignmentId)
            .IsRequired();

        builder.Property(q => q.QuizId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(q => q.TimeLimitSeconds)
            .IsRequired(false); // Nullable time limit

        builder.Property(q => q.ShuffleQuestions)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(q => q.PassingThreshold)
            .IsRequired(false); // Nullable passing threshold

        builder.Property(q => q.CreatedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(q => q.Assignment)
            .WithOne(a => a.Quiz)
            .HasForeignKey<Quiz>(q => q.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade); // Cascade delete if Assignment is deleted

        builder.HasMany(q => q.Questions)
            .WithOne(qq => qq.Quiz)
            .HasForeignKey(qq => qq.QuizId)
            .OnDelete(DeleteBehavior.Cascade);

        // Note: Sessions relationship is configured in QuizSessionConfiguration with NoAction to avoid cascade paths

        // Unique constraint: one Quiz per Assignment
        builder.HasIndex(q => q.AssignmentId)
            .IsUnique();
    }
}

