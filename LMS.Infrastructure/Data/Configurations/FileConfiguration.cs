using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Data.Configurations;

public class FileConfiguration : IEntityTypeConfiguration<LMS.Domain.Entities.File>
{
    public void Configure(EntityTypeBuilder<LMS.Domain.Entities.File> builder)
    {
        builder.ToTable("Files");

        builder.HasKey(f => f.Id);

        builder.Property(f => f.Url)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(f => f.FileName)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(f => f.ContentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(f => f.Size)
            .IsRequired();

        builder.Property(f => f.UploadedById)
            .IsRequired();

        builder.Property(f => f.UploadedAt)
            .IsRequired();

        builder.Property(f => f.CreatedAt)
            .IsRequired();

        // Index for faster lookups by uploaded user
        builder.HasIndex(f => f.UploadedById);
    }
}

