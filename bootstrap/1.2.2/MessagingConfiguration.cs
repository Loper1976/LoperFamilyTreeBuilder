using LoperFamilyTreeBuilder.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoperFamilyTreeBuilder.Data.Configuration;

public sealed class FamilyMessageConfiguration : IEntityTypeConfiguration<FamilyMessage>
{
    public void Configure(EntityTypeBuilder<FamilyMessage> builder)
    {
        builder.ToTable("FamilyMessages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ConversationKey).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Subject).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Body).HasColumnType("nvarchar(max)").IsRequired();
        builder.Ignore(x => x.IsArchived);
        builder.Ignore(x => x.AutomaticArchiveUtc);
        builder.HasIndex(x => new { x.RecipientUserId, x.ArchivedUtc, x.SentUtc });
        builder.HasIndex(x => new { x.SenderUserId, x.ArchivedUtc, x.SentUtc });
        builder.HasIndex(x => new { x.ConversationKey, x.SentUtc });
        builder.HasOne(x => x.SenderUser).WithMany().HasForeignKey(x => x.SenderUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.RecipientUser).WithMany().HasForeignKey(x => x.RecipientUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
