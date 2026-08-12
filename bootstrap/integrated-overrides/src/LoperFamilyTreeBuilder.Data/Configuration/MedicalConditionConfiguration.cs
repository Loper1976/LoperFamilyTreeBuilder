using LoperFamilyTreeBuilder.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoperFamilyTreeBuilder.Data.Configuration;

public sealed class MedicalConditionConfiguration :
    IEntityTypeConfiguration<MedicalCondition>
{
    public void Configure(EntityTypeBuilder<MedicalCondition> builder)
    {
        builder.ToTable("MedicalConditions");

        builder.HasKey(record => record.Id);

        builder.Property(record => record.ConditionName)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(record => record.Provider)
            .HasMaxLength(300);

        builder.Property(record => record.Facility)
            .HasMaxLength(300);

        builder.Property(record => record.Notes)
            .HasColumnType("nvarchar(max)");

        builder.Property(record => record.SourceCitation)
            .HasColumnType("nvarchar(max)");

        builder.Property(record => record.Status)
            .HasConversion<int>();

        builder.Property(record => record.Severity)
            .HasConversion<int>();

        builder.Property(record => record.Visibility)
            .HasConversion<int>();

        builder.HasOne(record => record.Person)
            .WithMany()
            .HasForeignKey(record => record.PersonId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(record => record.PersonId);
        builder.HasIndex(record => record.ConditionName);
        builder.HasIndex(record => record.IsHereditaryRelevant);
        builder.HasIndex(record => record.Visibility);
    }
}
