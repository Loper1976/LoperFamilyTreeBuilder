using LoperFamilyTreeBuilder.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoperFamilyTreeBuilder.Data.Configuration;

public sealed class ParentChildRelationshipConfiguration :
    IEntityTypeConfiguration<ParentChildRelationship>
{
    public void Configure(EntityTypeBuilder<ParentChildRelationship> builder)
    {
        builder.ToTable("ParentChildRelationships");

        builder.HasKey(relationship => relationship.Id);

        builder.Property(relationship => relationship.RelationshipType)
            .HasConversion<int>()
            .IsRequired();

        builder.HasIndex(relationship => relationship.ParentPersonId);
        builder.HasIndex(relationship => relationship.ChildPersonId);

        builder.HasIndex(relationship => new
        {
            relationship.ParentPersonId,
            relationship.ChildPersonId,
            relationship.RelationshipType
        })
        .IsUnique();

        builder.HasOne(relationship => relationship.ParentPerson)
            .WithMany()
            .HasForeignKey(relationship => relationship.ParentPersonId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(relationship => relationship.ChildPerson)
            .WithMany()
            .HasForeignKey(relationship => relationship.ChildPersonId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
