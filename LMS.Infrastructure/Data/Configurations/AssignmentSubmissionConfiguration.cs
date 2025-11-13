using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Data.Configurations;

public class AssignmentSubmissionConfiguration : IEntityTypeConfiguration<AssignmentSubmission>
{
    public void Configure(EntityTypeBuilder<AssignmentSubmission> builder)
    {
        builder.ToTable("AssignmentSubmissions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.AssignmentId)
            .IsRequired();

        builder.Property(s => s.StudentId)
            .IsRequired();

        builder.Property(s => s.FileUrl)
            .HasMaxLength(500);

        builder.Property(s => s.AnswerText)
            .HasMaxLength(5000);

        builder.Property(s => s.SubmittedAt)
            .IsRequired();

        builder.Property(s => s.Score)
            .IsRequired(false); // Score is nullable, int type

        builder.Property(s => s.Feedback)
            .HasMaxLength(2000)
            .IsRequired(false); // Feedback is nullable

        builder.Property(s => s.EvaluatedById)
            .IsRequired(false);

        builder.Property(s => s.EvaluatedAt)
            .IsRequired(false);

        builder.Property(s => s.TimeOnPageInSeconds)
            .IsRequired(false); // Time student spent on assignment page (aggregate)

        builder.Property(s => s.Passed)
            .IsRequired(false); // Whether assignment was passed (based on threshold)

        builder.Property(s => s.IsExcellent)
            .IsRequired(false); // Whether assignment score is above high threshold (excellent)

        builder.Property(s => s.PercentageScore)
            .IsRequired()
            .HasDefaultValue(0)
            .HasColumnType("decimal(5,2)"); // e.g., 85.50

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(s => s.Assignment)
            .WithMany(a => a.Submissions)
            .HasForeignKey(s => s.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

