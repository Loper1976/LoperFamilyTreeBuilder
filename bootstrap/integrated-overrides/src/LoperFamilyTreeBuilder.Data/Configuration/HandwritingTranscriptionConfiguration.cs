using LoperFamilyTreeBuilder.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LoperFamilyTreeBuilder.Data.Configuration;

public sealed class HandwritingTranscriptionConfiguration :
    IEntityTypeConfiguration<HandwritingTranscription>
{
    public void Configure(EntityTypeBuilder<HandwritingTranscription> builder)
    {
        builder.ToTable("HandwritingTranscriptions");

        builder.HasKey(record => record.Id);

        builder.Property(record => record.DocumentTitle)
            .HasMaxLength(300)
            .IsRequired();

        builder.Property(record => record.ArchiveRelativePath)
            .HasMaxLength(1024)
            .IsRequired();

        builder.Property(record => record.OriginalImageHashSha256)
            .HasMaxLength(64);

        builder.Property(record => record.SourceCitation)
            .HasColumnType("nvarchar(max)");

        builder.Property(record => record.Status)
            .HasConversion<int>();

        builder.Property(record => record.Visibility)
            .HasConversion<int>();

        builder.Property(record => record.ProviderName)
            .HasMaxLength(200);

        builder.Property(record => record.ModelName)
            .HasMaxLength(200);

        builder.Property(record => record.Confidence)
            .HasPrecision(5, 4);

        builder.Property(record => record.MachineDraft)
            .HasColumnType("nvarchar(max)");

        builder.Property(record => record.ReviewedTranscript)
            .HasColumnType("nvarchar(max)");

        builder.Property(record => record.ApprovedTranscript)
            .HasColumnType("nvarchar(max)");

        builder.Property(record => record.FailureMessage)
            .HasMaxLength(2000);

        builder.HasOne(record => record.Person)
            .WithMany()
            .HasForeignKey(record => record.PersonId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(record => record.PersonId);
        builder.HasIndex(record => record.Status);
        builder.HasIndex(record => record.Visibility);
        builder.HasIndex(record => record.ModifiedUtc);
    }
}
