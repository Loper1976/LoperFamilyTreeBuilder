using LoperFamilyTreeBuilder.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoperFamilyTreeBuilder.Data.Configuration;

public sealed class AuditEventConfiguration :
    IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.ToTable("AuditEvents");

        builder.HasKey(audit => audit.Id);

        builder.Property(audit => audit.Action)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(audit => audit.EntityType)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(audit => audit.EntityId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(audit => audit.Actor)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(audit => audit.Summary)
            .HasMaxLength(2000)
            .IsRequired();

        builder.Property(audit => audit.PreviousValueJson)
            .HasColumnType("nvarchar(max)");

        builder.Property(audit => audit.NewValueJson)
            .HasColumnType("nvarchar(max)");

        builder.HasIndex(audit => audit.OccurredUtc);
        builder.HasIndex(audit => audit.EntityType);
        builder.HasIndex(audit => audit.EntityId);
        builder.HasIndex(audit => audit.Action);
    }
}
