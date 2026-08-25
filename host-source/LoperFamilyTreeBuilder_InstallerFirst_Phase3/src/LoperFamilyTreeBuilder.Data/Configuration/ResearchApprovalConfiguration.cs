using LoperFamilyTreeBuilder.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoperFamilyTreeBuilder.Data.Configuration;

public sealed class ResearchApprovalConfiguration : IEntityTypeConfiguration<ResearchApproval>
{
    public void Configure(EntityTypeBuilder<ResearchApproval> builder)
    {
        builder.ToTable("ResearchApprovals");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EntityType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Decision).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Actor).HasMaxLength(250).IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(2000).IsRequired();
        builder.HasIndex(x => new { x.EntityId, x.Decision });
    }
}
