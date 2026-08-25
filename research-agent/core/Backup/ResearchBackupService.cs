using System.IO.Compression;
using ResearchAgent.Core.Domain;

namespace ResearchAgent.Core.Backup;

public sealed record BackupManifest(string SourceRoot, DateTimeOffset CreatedUtc, int FileCount,
    IReadOnlyDictionary<string, string> Sha256ByRelativePath);

public sealed record BackupResult(string ArchivePath, BackupManifest Manifest);

public sealed class ResearchBackupService
{
    public async Task<BackupResult> CreateAsync(string sourceRoot, string destinationDirectory,
        CancellationToken cancellationToken = default)
    {
        sourceRoot = Path.GetFullPath(sourceRoot);
        destinationDirectory = Path.GetFullPath(destinationDirectory);
        if (!Directory.Exists(sourceRoot)) throw new DirectoryNotFoundException(sourceRoot);
        Directory.CreateDirectory(destinationDirectory);

        var hashes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relative = Path.GetRelativePath(sourceRoot, file).Replace('\\', '/');
            await using var stream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read);
            hashes[relative] = await Hashing.Sha256Async(stream, cancellationToken);
        }

        var stamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");
        var archive = Path.Combine(destinationDirectory, $"research-backup-{stamp}.zip");
        ZipFile.CreateFromDirectory(sourceRoot, archive, CompressionLevel.Optimal, false);
        return new(archive, new(sourceRoot, DateTimeOffset.UtcNow, hashes.Count, hashes));
    }

    public async Task VerifyExtractedAsync(string extractedRoot, BackupManifest manifest,
        CancellationToken cancellationToken = default)
    {
        foreach (var pair in manifest.Sha256ByRelativePath)
        {
            var path = Path.Combine(extractedRoot, pair.Key.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path)) throw new InvalidDataException($"Backup verification failed: missing {pair.Key}.");
            await using var stream = File.OpenRead(path);
            var hash = await Hashing.Sha256Async(stream, cancellationToken);
            if (!hash.Equals(pair.Value, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Backup verification failed: hash mismatch for {pair.Key}.");
        }
    }
}
