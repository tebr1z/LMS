using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Data.Configurations;

public class SystemSettingsConfiguration : IEntityTypeConfiguration<SystemSettings>
{
    public void Configure(EntityTypeBuilder<SystemSettings> builder)
    {
        builder.ToTable("SystemSettings");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Key)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.Value)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(s => s.Description)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(s => s.Category)
            .IsRequired()
            .HasMaxLength(100)
            .HasDefaultValue("General");

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        // Unique constraint: Key must be unique
        builder.HasIndex(s => s.Key)
            .IsUnique();

        // Index for faster lookups by category
        builder.HasIndex(s => s.Category)
            .HasDatabaseName("IX_SystemSettings_Category");
    }
}

