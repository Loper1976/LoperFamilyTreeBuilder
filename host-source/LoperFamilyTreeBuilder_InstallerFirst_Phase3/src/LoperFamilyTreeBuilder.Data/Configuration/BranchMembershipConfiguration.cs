using LoperFamilyTreeBuilder.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoperFamilyTreeBuilder.Data.Configuration;

public sealed class BranchMembershipConfiguration :
    IEntityTypeConfiguration<BranchMembership>
{
    public void Configure(EntityTypeBuilder<BranchMembership> builder)
    {
        builder.ToTable("BranchMemberships");

        builder.HasKey(membership => membership.Id);

        builder.HasIndex(membership => new
        {
            membership.PersonId,
            membership.FamilyBranchId
        })
        .IsUnique();

        builder.HasIndex(membership => membership.FamilyBranchId);
    }
}
