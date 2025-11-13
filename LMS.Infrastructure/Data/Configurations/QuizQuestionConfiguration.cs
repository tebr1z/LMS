using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Data.Configurations;

public class QuizQuestionConfiguration : IEntityTypeConfiguration<QuizQuestion>
{
    public void Configure(EntityTypeBuilder<QuizQuestion> builder)
    {
        builder.ToTable("QuizQuestions");

        builder.HasKey(qq => qq.Id);

        builder.Property(qq => qq.QuizId)
            .IsRequired();

        builder.Property(qq => qq.Text)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(qq => qq.Options)
            .IsRequired()
            .HasColumnType("nvarchar(max)"); // JSON array

        builder.Property(qq => qq.CorrectAnswer)
            .IsRequired()
            .HasMaxLength(500); // Can be comma-separated for multiple correct answers

        builder.Property(qq => qq.Points)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(qq => qq.Order)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(qq => qq.CreatedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(qq => qq.Quiz)
            .WithMany(q => q.Questions)
            .HasForeignKey(qq => qq.QuizId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(qq => qq.Responses)
            .WithOne(qr => qr.Question)
            .HasForeignKey(qr => qr.QuizQuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

