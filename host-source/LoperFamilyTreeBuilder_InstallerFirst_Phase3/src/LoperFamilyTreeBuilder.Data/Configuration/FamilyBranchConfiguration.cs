using LoperFamilyTreeBuilder.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoperFamilyTreeBuilder.Data.Configuration;

public sealed class FamilyBranchConfiguration :
    IEntityTypeConfiguration<FamilyBranch>
{
    public void Configure(EntityTypeBuilder<FamilyBranch> builder)
    {
        builder.ToTable("FamilyBranches");

        builder.HasKey(branch => branch.Id);

        builder.Property(branch => branch.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(branch => branch.ShortCode)
            .HasMaxLength(50);

        builder.Property(branch => branch.Description)
            .HasMaxLength(2000);

        builder.Property(branch => branch.NumberingPolicyName)
            .HasMaxLength(200);

        builder.HasIndex(branch => branch.Name);
        builder.HasIndex(branch => branch.ShortCode);

        builder.HasMany(branch => branch.Memberships)
            .WithOne(membership => membership.FamilyBranch)
            .HasForeignKey(membership => membership.FamilyBranchId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
