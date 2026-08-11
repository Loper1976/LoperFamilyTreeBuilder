using LoperFamilyTreeBuilder.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoperFamilyTreeBuilder.Data.Configuration;

public sealed class MediaMigrationSessionConfiguration : IEntityTypeConfiguration<MediaMigrationSession>
{
    public void Configure(EntityTypeBuilder<MediaMigrationSession> builder)
    {
        builder.ToTable("MediaMigrationSessions");
        builder.HasKey(x=>x.Id);
        builder.Property(x=>x.SessionCode).HasMaxLength(64).IsRequired();
        builder.Property(x=>x.SourceType).HasConversion<int>();
        builder.Property(x=>x.SourceRootPath).HasMaxLength(2000).IsRequired();
        builder.Property(x=>x.StartedBy).HasMaxLength(500).IsRequired();
        builder.Property(x=>x.Status).HasConversion<int>();
        builder.HasIndex(x=>x.SessionCode).IsUnique();
        builder.HasIndex(x=>x.CreatedUtc);
    }
}

public sealed class MediaMigrationItemConfiguration : IEntityTypeConfiguration<MediaMigrationItem>
{
    public void Configure(EntityTypeBuilder<MediaMigrationItem> builder)
    {
        builder.ToTable("MediaMigrationItems");
        builder.HasKey(x=>x.Id);
        builder.Property(x=>x.SourceRelativePath).HasMaxLength(2000).IsRequired();
        builder.Property(x=>x.OriginalFileName).HasMaxLength(1000).IsRequired();
        builder.Property(x=>x.Sha256).HasMaxLength(64).IsRequired();
        builder.Property(x=>x.MediaType).HasConversion<int>();
        builder.Property(x=>x.MimeType).HasMaxLength(250).IsRequired();
        builder.Property(x=>x.CapturedMetadataJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x=>x.Status).HasConversion<int>();
        builder.Property(x=>x.SuggestedMatchReason).HasMaxLength(1000).IsRequired();
        builder.Property(x=>x.ReviewNote).HasMaxLength(2000).IsRequired();
        builder.HasIndex(x=>new{x.SessionId,x.Status});
        builder.HasIndex(x=>x.Sha256);
        builder.HasOne(x=>x.Session).WithMany().HasForeignKey(x=>x.SessionId).OnDelete(DeleteBehavior.Cascade);
    }
}
