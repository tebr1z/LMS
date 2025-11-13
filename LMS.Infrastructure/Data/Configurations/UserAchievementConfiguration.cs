using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Data.Configurations;

public class UserAchievementConfiguration : IEntityTypeConfiguration<UserAchievement>
{
    public void Configure(EntityTypeBuilder<UserAchievement> builder)
    {
        builder.ToTable("UserAchievements");

        builder.HasKey(ua => ua.Id);

        builder.Property(ua => ua.UserId)
            .IsRequired();

        builder.Property(ua => ua.AchievementId)
            .IsRequired();

        builder.Property(ua => ua.EarnedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(ua => ua.Achievement)
            .WithMany(a => a.UserAchievements)
            .HasForeignKey(ua => ua.AchievementId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique constraint: one achievement per user (can't earn same achievement twice)
        builder.HasIndex(ua => new { ua.UserId, ua.AchievementId })
            .IsUnique()
            .HasDatabaseName("IX_UserAchievements_UserId_AchievementId");

        // Indexes for faster lookups
        builder.HasIndex(ua => ua.UserId)
            .HasDatabaseName("IX_UserAchievements_UserId");

        builder.HasIndex(ua => ua.EarnedAt)
            .HasDatabaseName("IX_UserAchievements_EarnedAt");
    }
}


