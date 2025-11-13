using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Data.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.UserId)
            .IsRequired();

        builder.Property(n => n.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(n => n.Body)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(n => n.Data)
            .HasMaxLength(5000)
            .IsRequired(false); // JSON data

        builder.Property(n => n.IsRead)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(n => n.CreatedAt)
            .IsRequired();

        builder.Property(n => n.ReadAt)
            .IsRequired(false);

        builder.Property(n => n.Channel)
            .IsRequired()
            .HasConversion<int>()
            .HasDefaultValue(Domain.Enums.NotificationChannel.InApp)
            .HasSentinel(Domain.Enums.NotificationChannel.InApp); // Sentinel value for default

        builder.Property(n => n.Type)
            .HasMaxLength(100)
            .IsRequired(false);

        // Indexes for faster lookups
        builder.HasIndex(n => new { n.UserId, n.IsRead })
            .HasDatabaseName("IX_Notifications_User_IsRead");

        builder.HasIndex(n => n.CreatedAt)
            .HasDatabaseName("IX_Notifications_CreatedAt");

        builder.HasIndex(n => n.Type)
            .HasDatabaseName("IX_Notifications_Type");
    }
}

