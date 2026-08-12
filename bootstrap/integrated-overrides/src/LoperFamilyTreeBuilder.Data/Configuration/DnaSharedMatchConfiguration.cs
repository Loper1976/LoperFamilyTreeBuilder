using LoperFamilyTreeBuilder.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoperFamilyTreeBuilder.Data.Configuration;

public sealed class DnaSharedMatchConfiguration : IEntityTypeConfiguration<DnaSharedMatch>
{
    public void Configure(EntityTypeBuilder<DnaSharedMatch> builder)
    {
        builder.ToTable("DnaSharedMatches");
        builder.HasKey(edge => edge.Id);

        builder.Property(edge => edge.EvidenceSource)
            .HasMaxLength(500)
            .IsRequired();

        builder.HasOne(edge => edge.MatchA)
            .WithMany()
            .HasForeignKey(edge => edge.MatchAId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(edge => edge.MatchB)
            .WithMany()
            .HasForeignKey(edge => edge.MatchBId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(edge => new { edge.MatchAId, edge.MatchBId })
            .IsUnique();
        builder.HasIndex(edge => edge.CreatedUtc);
    }
}
