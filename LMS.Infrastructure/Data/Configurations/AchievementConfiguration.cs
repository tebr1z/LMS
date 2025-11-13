using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Data.Configurations;

public class AchievementConfiguration : IEntityTypeConfiguration<Achievement>
{
    public void Configure(EntityTypeBuilder<Achievement> builder)
    {
        builder.ToTable("Achievements");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.Description)
            .HasMaxLength(1000);

        builder.Property(a => a.CriteriaJson)
            .HasMaxLength(2000); // JSON string for achievement criteria

        builder.Property(a => a.IconUrl)
            .HasMaxLength(500);

        builder.Property(a => a.PointsReward)
            .IsRequired()
            .HasDefaultValue(0);

        builder.HasIndex(a => a.Name)
            .HasDatabaseName("IX_Achievements_Name");
    }
}


