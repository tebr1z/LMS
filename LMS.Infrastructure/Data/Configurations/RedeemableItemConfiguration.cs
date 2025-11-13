using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Data.Configurations;

public class RedeemableItemConfiguration : IEntityTypeConfiguration<RedeemableItem>
{
    public void Configure(EntityTypeBuilder<RedeemableItem> builder)
    {
        builder.ToTable("RedeemableItems");

        builder.HasKey(ri => ri.Id);

        builder.Property(ri => ri.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(ri => ri.Description)
            .HasMaxLength(1000);

        builder.Property(ri => ri.CostPoints)
            .IsRequired();

        builder.Property(ri => ri.BenefitType)
            .IsRequired()
            .HasMaxLength(100); // e.g., "ExtraCredit", "SkipAssignment", "Certificate"

        builder.Property(ri => ri.BenefitValue)
            .HasMaxLength(2000); // JSON string for benefit-specific data

        builder.Property(ri => ri.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Index for active items
        builder.HasIndex(ri => ri.IsActive)
            .HasDatabaseName("IX_RedeemableItems_IsActive");

        builder.HasIndex(ri => ri.CostPoints)
            .HasDatabaseName("IX_RedeemableItems_CostPoints");
    }
}


