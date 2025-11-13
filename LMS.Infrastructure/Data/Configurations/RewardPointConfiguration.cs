using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Data.Configurations;

public class RewardPointConfiguration : IEntityTypeConfiguration<RewardPoint>
{
    public void Configure(EntityTypeBuilder<RewardPoint> builder)
    {
        builder.ToTable("RewardPoints");

        builder.HasKey(rp => rp.Id);

        builder.Property(rp => rp.UserId)
            .IsRequired();

        builder.Property(rp => rp.Points)
            .IsRequired();

        builder.Property(rp => rp.Reason)
            .IsRequired()
            .HasMaxLength(500);

        // Note: CreatedAt is inherited from BaseEntity
        builder.Property(rp => rp.CreatedAt)
            .IsRequired();

        // Indexes for faster lookups
        builder.HasIndex(rp => rp.UserId)
            .HasDatabaseName("IX_RewardPoints_UserId");

        builder.HasIndex(rp => rp.CreatedAt)
            .HasDatabaseName("IX_RewardPoints_CreatedAt");
    }
}

