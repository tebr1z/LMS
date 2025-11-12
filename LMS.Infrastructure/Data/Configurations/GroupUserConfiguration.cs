using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Data.Configurations;

public class GroupUserConfiguration : IEntityTypeConfiguration<GroupUser>
{
    public void Configure(EntityTypeBuilder<GroupUser> builder)
    {
        builder.ToTable("GroupUsers");

        builder.HasKey(gu => gu.Id);

        builder.Property(gu => gu.GroupId)
            .IsRequired();

        builder.Property(gu => gu.UserId)
            .IsRequired();

        builder.Property(gu => gu.Role)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(gu => gu.CreatedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(gu => gu.Group)
            .WithMany(g => g.GroupUsers)
            .HasForeignKey(gu => gu.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint: A user can only have one role per group
        builder.HasIndex(gu => new { gu.GroupId, gu.UserId })
            .IsUnique();
    }
}

