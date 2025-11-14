using LMS.Domain.Entities;
using LMS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Data.Configurations;

public class NotificationRuleConfiguration : IEntityTypeConfiguration<NotificationRule>
{
    public void Configure(EntityTypeBuilder<NotificationRule> builder)
    {
        builder.ToTable("NotificationRules");

        builder.HasKey(nr => nr.Id);

        builder.Property(nr => nr.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(nr => nr.ConditionJson)
            .IsRequired()
            .HasColumnType("nvarchar(max)"); // Store as JSON string

        builder.Property(nr => nr.TargetRole)
            .IsRequired()
            .HasConversion<int>(); // Store enum as int

        builder.Property(nr => nr.MessageTemplate)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(nr => nr.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(nr => nr.Type)
            .HasMaxLength(100);

        builder.Property(nr => nr.Priority)
            .HasDefaultValue(5);

        builder.Property(nr => nr.CreatedAt)
            .IsRequired();

        // Indexes for faster lookups
        builder.HasIndex(nr => nr.IsActive)
            .HasDatabaseName("IX_NotificationRules_IsActive");

        builder.HasIndex(nr => nr.TargetRole)
            .HasDatabaseName("IX_NotificationRules_TargetRole");

        builder.HasIndex(nr => nr.Type)
            .HasDatabaseName("IX_NotificationRules_Type");
    }
}


