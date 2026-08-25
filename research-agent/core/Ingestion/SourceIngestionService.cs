using ResearchAgent.Core.Domain;

namespace ResearchAgent.Core.Ingestion;

public sealed record SourceIngestionRequest(
    string InputPath, string ArchiveRoot, string Title, string? SourceType,
    string? RepositoryName, Uri? OriginalUrl, PrivacyClass PrivacyClass,
    DateTimeOffset? RetrievedUtc = null, string? Notes = null);

public sealed class SourceIngestionService
{
    public async Task<ResearchSource> IngestAsync(SourceIngestionRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var input = Path.GetFullPath(request.InputPath);
        var archiveRoot = Path.GetFullPath(request.ArchiveRoot);

        if (!File.Exists(input)) throw new FileNotFoundException("Source file was not found.", input);
        Directory.CreateDirectory(archiveRoot);

        string hash;
        await using (var source = File.Open(input, FileMode.Open, FileAccess.Read, FileShare.Read))
            hash = await Hashing.Sha256Async(source, cancellationToken).ConfigureAwait(false);

        var extension = Path.GetExtension(input).ToLowerInvariant();
        var safeName = SanitizeFileName(Path.GetFileNameWithoutExtension(input));
        var relative = Path.Combine(hash[..2], hash[2..4], $"{hash}_{safeName}{extension}");
        var destination = Path.Combine(archiveRoot, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

        if (File.Exists(destination))
        {
            await VerifyHashAsync(destination, hash, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            File.Copy(input, destination, overwrite: false);
            await VerifyHashAsync(destination, hash, cancellationToken).ConfigureAwait(false);
            TryMakeReadOnly(destination);
        }

        var info = new FileInfo(destination);
        return new ResearchSource(
            Guid.NewGuid(), request.Title, request.SourceType, request.RepositoryName,
            request.OriginalUrl, request.RetrievedUtc ?? DateTimeOffset.UtcNow,
            Path.GetFileName(input), relative.Replace('\\', '/'), hash,
            GuessMimeType(extension), info.Length, request.PrivacyClass, true, request.Notes);
    }

    public async Task VerifyAsync(string archiveRoot, ResearchSource source, CancellationToken cancellationToken = default)
    {
        var path = Path.Combine(Path.GetFullPath(archiveRoot), source.ArchivedRelativePath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(path)) throw new FileNotFoundException("Archived source is missing.", path);
        await VerifyHashAsync(path, source.Sha256, cancellationToken).ConfigureAwait(false);
    }

    private static async Task VerifyHashAsync(string path, string expectedHash, CancellationToken cancellationToken)
    {
        await using var stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        var actual = await Hashing.Sha256Async(stream, cancellationToken).ConfigureAwait(false);
        if (!string.Equals(actual, expectedHash, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException($"Source integrity check failed for '{path}'. Expected {expectedHash}, found {actual}.");
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars().ToHashSet();
        var chars = name.Select(c => invalid.Contains(c) ? '_' : c).ToArray();
        var result = new string(chars).Trim();
        return string.IsNullOrWhiteSpace(result) ? "source" : result[..Math.Min(result.Length, 80)];
    }

    private static void TryMakeReadOnly(string path)
    {
        try { File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.ReadOnly); }
        catch (UnauthorizedAccessException) { }
        catch (IOException) { }
    }

    private static string? GuessMimeType(string extension) => extension switch
    {
        ".pdf" => "application/pdf",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png" => "image/png",
        ".tif" or ".tiff" => "image/tiff",
        ".txt" => "text/plain",
        ".csv" => "text/csv",
        ".json" => "application/json",
        ".ged" => "text/vnd.familysearch.gedcom",
        ".gedzip" => "application/zip",
        _ => null
    };
}
