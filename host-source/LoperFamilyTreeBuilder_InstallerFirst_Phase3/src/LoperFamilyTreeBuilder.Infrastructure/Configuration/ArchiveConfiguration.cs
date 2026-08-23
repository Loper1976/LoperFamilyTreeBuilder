namespace LoperFamilyTreeBuilder.Infrastructure.Configuration;

public sealed class ArchiveConfiguration
{
    public int SchemaVersion { get; set; } = 1;

    public string PrimaryArchivePath { get; set; } = string.Empty;

    public string BackupPath { get; set; } = string.Empty;

    public DateTimeOffset ConfiguredUtc { get; set; } = DateTimeOffset.UtcNow;

    public bool IsComplete =>
        !string.IsNullOrWhiteSpace(PrimaryArchivePath)
        && !string.IsNullOrWhiteSpace(BackupPath);
}
