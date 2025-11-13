using LMS.Domain.Entities;
using LMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Data.Configurations;

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.StudentId)
            .IsRequired();

        builder.Property(p => p.Amount)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(p => p.Currency)
            .IsRequired()
            .HasMaxLength(10)
            .HasDefaultValue("AZN");

        builder.Property(p => p.PaymentMethod)
            .IsRequired()
            .HasConversion<int>(); // Store enum as int

        builder.Property(p => p.Status)
            .IsRequired()
            .HasConversion<int>() // Store enum as int
            .HasDefaultValue(PaymentStatus.Pending) // Default: Pending
            .HasSentinel(PaymentStatus.Pending); // Sentinel value for default

        builder.Property(p => p.PaidAt)
            .IsRequired(false);

        builder.Property(p => p.DueDate)
            .IsRequired(false);

        builder.Property(p => p.Reference)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(p => p.InvoiceId)
            .IsRequired(false);

        builder.Property(p => p.ProviderTransactionId)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.Property(p => p.Notes)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(p => p.CreatedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(p => p.Invoice)
            .WithMany(i => i.Payments)
            .HasForeignKey(p => p.InvoiceId)
            .OnDelete(DeleteBehavior.SetNull); // Set null if invoice is deleted

        // Indexes for faster lookups
        builder.HasIndex(p => new { p.StudentId, p.Status })
            .HasDatabaseName("IX_Payments_Student_Status");

        builder.HasIndex(p => p.DueDate)
            .HasDatabaseName("IX_Payments_DueDate");

        builder.HasIndex(p => p.ProviderTransactionId)
            .HasDatabaseName("IX_Payments_ProviderTransactionId")
            .IsUnique()
            .HasFilter("[ProviderTransactionId] IS NOT NULL");
    }
}

