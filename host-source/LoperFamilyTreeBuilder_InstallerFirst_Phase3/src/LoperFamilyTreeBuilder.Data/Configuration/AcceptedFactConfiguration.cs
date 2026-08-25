using LoperFamilyTreeBuilder.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoperFamilyTreeBuilder.Data.Configuration;

public sealed class AcceptedFactConfiguration : IEntityTypeConfiguration<AcceptedFact>
{
    public void Configure(EntityTypeBuilder<AcceptedFact> builder)
    {
        builder.ToTable("AcceptedFacts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FactType).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Value).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.AcceptedBy).HasMaxLength(250).IsRequired();
        builder.HasIndex(x => x.ResearchClaimId).IsUnique();
        builder.HasIndex(x => new { x.PersonId, x.FactType });
        builder.HasOne(x => x.Person).WithMany().HasForeignKey(x => x.PersonId).OnDelete(DeleteBehavior.Restrict);
    }
}
