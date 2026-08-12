using LoperFamilyTreeBuilder.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoperFamilyTreeBuilder.Data.Configuration;

public sealed class TreeIntegrityIssueConfiguration : IEntityTypeConfiguration<TreeIntegrityIssue>
{
    public void Configure(EntityTypeBuilder<TreeIntegrityIssue> builder)
    {
        builder.ToTable("TreeIntegrityIssues");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.IssueKey).HasMaxLength(500).IsRequired();
        builder.Property(x => x.IssueType).HasConversion<int>();
        builder.Property(x => x.Severity).HasConversion<int>();
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.Title).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Description).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.EvidenceSummary).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.ReviewReason).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.ReviewedBy).HasMaxLength(500).IsRequired();
        builder.HasIndex(x => x.IssueKey).IsUnique();
        builder.HasIndex(x => new { x.IsActive, x.Severity, x.Status });
        builder.HasIndex(x => x.PersonId);
        builder.HasIndex(x => x.RelatedPersonId);
        builder.HasIndex(x => x.LastDetectedUtc);
    }
}

public sealed class TreeIntegrityScanRunConfiguration : IEntityTypeConfiguration<TreeIntegrityScanRun>
{
    public void Configure(EntityTypeBuilder<TreeIntegrityScanRun> builder)
    {
        builder.ToTable("TreeIntegrityScanRuns");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.StartedBy).HasMaxLength(500).IsRequired();
        builder.Property(x => x.RulesVersion).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(50).IsRequired();
        builder.HasIndex(x => x.StartedUtc);
        builder.HasIndex(x => x.CompletedUtc);
    }
}
