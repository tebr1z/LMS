using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Data.Configurations;

public class QuizResponseConfiguration : IEntityTypeConfiguration<QuizResponse>
{
    public void Configure(EntityTypeBuilder<QuizResponse> builder)
    {
        builder.ToTable("QuizResponses");

        builder.HasKey(qr => qr.Id);

        builder.Property(qr => qr.StudentId)
            .IsRequired();

        builder.Property(qr => qr.QuizId)
            .IsRequired();

        builder.Property(qr => qr.QuizQuestionId)
            .IsRequired();

        builder.Property(qr => qr.QuizSessionId)
            .IsRequired();

        builder.Property(qr => qr.SelectedOption)
            .IsRequired()
            .HasMaxLength(500); // Can be comma-separated for multiple selections

        builder.Property(qr => qr.AnswerText)
            .HasMaxLength(2000); // For open-ended questions

        builder.Property(qr => qr.IsCorrect)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(qr => qr.PointsAwarded)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(qr => qr.AnsweredAt)
            .IsRequired();

        builder.Property(qr => qr.CreatedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(qr => qr.Question)
            .WithMany(qq => qq.Responses)
            .HasForeignKey(qr => qr.QuizQuestionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(qr => qr.Session)
            .WithMany(qs => qs.Responses)
            .HasForeignKey(qr => qr.QuizSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index for faster lookups
        builder.HasIndex(qr => new { qr.QuizSessionId, qr.QuizQuestionId })
            .IsUnique(); // One response per question per session
    }
}

