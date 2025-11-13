using LMS.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LMS.Infrastructure.Data.Configurations;

public class LiveSessionConfiguration : IEntityTypeConfiguration<LiveSession>
{
    public void Configure(EntityTypeBuilder<LiveSession> builder)
    {
        builder.ToTable("LiveSessions");

        builder.HasKey(ls => ls.Id);

        builder.Property(ls => ls.GroupId)
            .IsRequired();

        builder.Property(ls => ls.TeacherId)
            .IsRequired();

        builder.Property(ls => ls.StartTime)
            .IsRequired();

        builder.Property(ls => ls.SessionUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(ls => ls.RecordingUrl)
            .HasMaxLength(500);

        builder.Property(ls => ls.JwtToken)
            .HasMaxLength(2000); // JWT tokens can be long

        builder.Property(ls => ls.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // Relationships
        builder.HasOne(ls => ls.Group)
            .WithMany()
            .HasForeignKey(ls => ls.GroupId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes for faster lookups
        builder.HasIndex(ls => new { ls.GroupId, ls.IsActive, ls.EndTime })
            .HasDatabaseName("IX_LiveSessions_GroupId_IsActive_EndTime");

        builder.HasIndex(ls => ls.TeacherId)
            .HasDatabaseName("IX_LiveSessions_TeacherId");

        builder.HasIndex(ls => ls.StartTime)
            .HasDatabaseName("IX_LiveSessions_StartTime");
    }
}

