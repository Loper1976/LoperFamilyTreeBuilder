using LoperFamilyTreeBuilder.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoperFamilyTreeBuilder.Data.Configuration;

public sealed class FamilyUnionConfiguration : IEntityTypeConfiguration<FamilyUnion>
{
    public void Configure(EntityTypeBuilder<FamilyUnion> builder)
    {
        builder.ToTable("FamilyUnions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PlaceText).HasMaxLength(500);
        builder.Property(x => x.SourceCitation).HasMaxLength(2000);
        builder.HasIndex(x => x.Person1Id);
        builder.HasIndex(x => x.Person2Id);
        builder.HasIndex(x => new { x.Person1Id, x.Person2Id });
    }
}

public sealed class LifeEventConfiguration : IEntityTypeConfiguration<LifeEvent>
{
    public void Configure(EntityTypeBuilder<LifeEvent> builder)
    {
        builder.ToTable("LifeEvents");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(300).IsRequired();
        builder.Property(x => x.OriginalPlaceText).HasMaxLength(500);
        builder.Property(x => x.Latitude).HasPrecision(9, 6);
        builder.Property(x => x.Longitude).HasPrecision(9, 6);
        builder.Property(x => x.SourceCitation).HasMaxLength(2000);
        builder.HasIndex(x => new { x.PersonId, x.StartDate });
    }
}

public sealed class ArchiveItemConfiguration : IEntityTypeConfiguration<ArchiveItem>
{
    public void Configure(EntityTypeBuilder<ArchiveItem> builder)
    {
        builder.ToTable("ArchiveItems");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(300).IsRequired();
        builder.Property(x => x.OriginalPath).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Sha256).HasMaxLength(128);
        builder.Property(x => x.OriginalPlaceText).HasMaxLength(500);
        builder.Property(x => x.Latitude).HasPrecision(9, 6);
        builder.Property(x => x.Longitude).HasPrecision(9, 6);
        builder.HasIndex(x => x.PersonId);
        builder.HasIndex(x => x.SourceRecordId);
        builder.HasIndex(x => x.ItemType);
    }
}

public sealed class SourceRecordConfiguration : IEntityTypeConfiguration<SourceRecord>
{
    public void Configure(EntityTypeBuilder<SourceRecord> builder)
    {
        builder.ToTable("SourceRecords");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Citation).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.Repository).HasMaxLength(500);
        builder.Property(x => x.CallNumberOrUrl).HasMaxLength(2000);
        builder.HasIndex(x => x.PersonId);
    }
}
