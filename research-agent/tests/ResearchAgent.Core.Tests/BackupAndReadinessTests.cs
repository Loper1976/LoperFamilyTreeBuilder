using System.IO.Compression;
using ResearchAgent.Core.Backup;
using ResearchAgent.Core.UI;

namespace ResearchAgent.Core.Tests;

public sealed class BackupAndReadinessTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "loper-backup-tests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task BackupCanBeVerifiedAfterRestore()
    {
        var source = Path.Combine(_root, "source");
        var backups = Path.Combine(_root, "backups");
        var restore = Path.Combine(_root, "restore");
        Directory.CreateDirectory(source);
        await File.WriteAllTextAsync(Path.Combine(source, "fictional.txt"), "fictional genealogy fixture");
        var service = new ResearchBackupService();
        var backup = await service.CreateAsync(source, backups);
        ZipFile.ExtractToDirectory(backup.ArchivePath, restore);
        await service.VerifyExtractedAsync(restore, backup.Manifest);
    }

    [Fact]
    public void AlphaGateRefusesPrematureInstallReadiness()
    {
        var result = AlphaReadiness.Evaluate(new(false, false, false, false, true, true, true, true, false, false, false, true));
        Assert.False(result.Ready);
        Assert.Contains("Installer build validation", result.Missing);
        Assert.Contains("Clean Windows bundle installation validation", result.Missing);
    }

    [Fact]
    public void AlphaGatePassesWhenEveryDemonstratedFlagIsTrue()
    {
        var result = AlphaReadiness.Evaluate(new(true, true, true, true, true, true, true, true, true, true, true, true));

        Assert.True(result.Ready);
        Assert.Empty(result.Missing);
    }

    public void Dispose()
    {
        try { if (Directory.Exists(_root)) Directory.Delete(_root, true); } catch { }
    }
}
