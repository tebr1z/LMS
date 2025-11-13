using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Data.Configurations;

public class AssignmentFeedbackAIConfiguration : IEntityTypeConfiguration<AssignmentFeedbackAI>
{
    public void Configure(EntityTypeBuilder<AssignmentFeedbackAI> builder)
    {
        builder.ToTable("AssignmentFeedbackAI");

        builder.HasKey(af => af.Id);

        builder.Property(af => af.SubmissionId)
            .IsRequired();

        builder.Property(af => af.AIComment)
            .IsRequired()
            .HasMaxLength(2000); // Max 3 sentences

        builder.Property(af => af.ConfidenceScore)
            .HasPrecision(5, 4); // 0.0000 to 1.0000

        builder.Property(af => af.CreatedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(af => af.Submission)
            .WithMany()
            .HasForeignKey(af => af.SubmissionId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent cascade delete

        // Indexes for faster lookups
        builder.HasIndex(af => af.SubmissionId)
            .HasDatabaseName("IX_AssignmentFeedbackAI_SubmissionId");

        builder.HasIndex(af => af.CreatedAt)
            .HasDatabaseName("IX_AssignmentFeedbackAI_CreatedAt");
    }
}

