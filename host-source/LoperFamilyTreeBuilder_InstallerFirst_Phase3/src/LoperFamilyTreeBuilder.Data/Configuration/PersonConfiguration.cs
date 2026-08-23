using LoperFamilyTreeBuilder.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoperFamilyTreeBuilder.Data.Configuration;

public sealed class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.ToTable("People");

        builder.HasKey(person => person.Id);

        builder.Property(person => person.GivenName)
            .HasMaxLength(200);

        builder.Property(person => person.MiddleName)
            .HasMaxLength(200);

        builder.Property(person => person.Surname)
            .HasMaxLength(200);

        builder.Property(person => person.Suffix)
            .HasMaxLength(100);

        builder.Property(person => person.CreatedUtc)
            .IsRequired();

        builder.Property(person => person.ModifiedUtc)
            .IsRequired();

        builder.HasIndex(person => person.Surname);
        builder.HasIndex(person => person.GivenName);
        builder.HasIndex(person => person.IsLiving);

        builder.HasMany(person => person.Identifiers)
            .WithOne(identifier => identifier.Person)
            .HasForeignKey(identifier => identifier.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(person => person.BranchMemberships)
            .WithOne(membership => membership.Person)
            .HasForeignKey(membership => membership.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(person => person.DisplayName);
    }
}
