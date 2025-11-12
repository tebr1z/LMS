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
            .HasPrecision(5, 2)
            .HasDefaultValue(null); // Score is nullable, range 0-100 // e.g., 100.00

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(s => s.Assignment)
            .WithMany(a => a.Submissions)
            .HasForeignKey(s => s.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

