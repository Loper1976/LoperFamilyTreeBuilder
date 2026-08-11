using LoperFamilyTreeBuilder.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoperFamilyTreeBuilder.Data.Configuration;

public sealed class GedcomImportedNoteConfiguration : IEntityTypeConfiguration<GedcomImportedNote>
{
    public void Configure(EntityTypeBuilder<GedcomImportedNote> builder)
    {
        builder.ToTable("GedcomImportedNotes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RecordPointer).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Text).HasColumnType("nvarchar(max)").IsRequired();
        builder.HasIndex(x => x.ImportSessionId);
        builder.HasIndex(x => x.PersonId);
        builder.HasOne(x => x.ImportSession).WithMany().HasForeignKey(x => x.ImportSessionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Person).WithMany().HasForeignKey(x => x.PersonId).OnDelete(DeleteBehavior.SetNull);
    }
}
