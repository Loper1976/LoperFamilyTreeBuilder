using LoperFamilyTreeBuilder.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoperFamilyTreeBuilder.Data.Configuration;

public sealed class DnaMatchConfiguration : IEntityTypeConfiguration<DnaMatch>
{
    public void Configure(EntityTypeBuilder<DnaMatch> builder)
    {
        builder.ToTable("DnaMatches");
        builder.HasKey(match => match.Id);

        builder.Property(match => match.ProviderName)
            .HasMaxLength(100)
            .IsRequired();
        builder.Property(match => match.ExternalMatchId)
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(match => match.DisplayName)
            .HasMaxLength(300)
            .IsRequired();
        builder.Property(match => match.TotalCentimorgans)
            .HasPrecision(8, 2);
        builder.Property(match => match.Visibility)
            .HasConversion<int>();
        builder.Property(match => match.ReviewStatus)
            .HasConversion<int>();
        builder.Property(match => match.ManualAncestralLineLabel)
            .HasMaxLength(300);
        builder.Property(match => match.ResearchNotes)
            .HasColumnType("nvarchar(max)");

        builder.HasIndex(match => new { match.ProviderName, match.ExternalMatchId })
            .IsUnique();
        builder.HasIndex(match => match.Visibility);
        builder.HasIndex(match => match.ReviewStatus);
        builder.HasIndex(match => match.TotalCentimorgans);
    }
}
