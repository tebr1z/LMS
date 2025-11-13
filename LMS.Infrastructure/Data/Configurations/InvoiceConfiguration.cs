using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Data.Configurations;

public class InvoiceConfiguration : IEntityTypeConfiguration<Invoice>
{
    public void Configure(EntityTypeBuilder<Invoice> builder)
    {
        builder.ToTable("Invoices");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.StudentId)
            .IsRequired();

        builder.Property(i => i.Amount)
            .IsRequired()
            .HasColumnType("decimal(18,2)");

        builder.Property(i => i.Currency)
            .IsRequired()
            .HasMaxLength(10)
            .HasDefaultValue("AZN");

        builder.Property(i => i.DueDate)
            .IsRequired();

        builder.Property(i => i.PaidStatus)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(i => i.PaidAt)
            .IsRequired(false);

        builder.Property(i => i.InvoiceNumber)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(i => i.Description)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(i => i.CreatedAt)
            .IsRequired();

        // Relationships
        builder.HasMany(i => i.Payments)
            .WithOne(p => p.Invoice)
            .HasForeignKey(p => p.InvoiceId)
            .OnDelete(DeleteBehavior.SetNull);

        // Unique constraint: InvoiceNumber must be unique
        builder.HasIndex(i => i.InvoiceNumber)
            .IsUnique();

        // Indexes for faster lookups
        builder.HasIndex(i => new { i.StudentId, i.PaidStatus })
            .HasDatabaseName("IX_Invoices_Student_PaidStatus");

        builder.HasIndex(i => i.DueDate)
            .HasDatabaseName("IX_Invoices_DueDate");
    }
}

