using LoperFamilyTreeBuilder.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoperFamilyTreeBuilder.Data.Configuration;

public sealed class PersonIdentifierConfiguration :
    IEntityTypeConfiguration<PersonIdentifier>
{
    public void Configure(EntityTypeBuilder<PersonIdentifier> builder)
    {
        builder.ToTable("PersonIdentifiers");

        builder.HasKey(identifier => identifier.Id);

        builder.Property(identifier => identifier.IdentifierType)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(identifier => identifier.Value)
            .HasMaxLength(255)
            .UseCollation("Latin1_General_100_BIN2")
            .IsRequired();

        builder.Property(identifier => identifier.IsProtected)
            .IsRequired();

        builder.Property(identifier => identifier.CreatedUtc)
            .IsRequired();

        builder.HasIndex(identifier => new
        {
            identifier.IdentifierType,
            identifier.Value
        });

        builder.HasIndex(identifier => identifier.PersonId);

        builder.HasIndex(identifier => identifier.Value);
    }
}
