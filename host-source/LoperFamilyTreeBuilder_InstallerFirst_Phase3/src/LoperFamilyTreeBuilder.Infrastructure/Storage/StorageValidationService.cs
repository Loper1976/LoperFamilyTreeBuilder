namespace LoperFamilyTreeBuilder.Infrastructure.Storage;

public sealed class StorageValidationService
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

    public async Task<StorageValidationResult> ValidateWritableFolderAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return new StorageValidationResult(
                false,
                path,
                "A folder must be selected.");
        }

        var work = Task.Run(
            () => ValidateWritableFolder(path),
            CancellationToken.None);

        var delay = Task.Delay(DefaultTimeout, cancellationToken);
        var completed = await Task.WhenAny(work, delay);

        if (completed != work)
        {
            return new StorageValidationResult(
                false,
                path,
                "The folder did not respond within five seconds. It may be a disconnected or unavailable network location.");
        }

        return await work;
    }

    private static StorageValidationResult ValidateWritableFolder(string path)
    {
        try
        {
            Directory.CreateDirectory(path);

            var probeFile = Path.Combine(
                path,
                $".loper-write-test-{Guid.NewGuid():N}.tmp");

            File.WriteAllText(probeFile, "Loper Family Tree Builder storage validation.");
            File.Delete(probeFile);

            return new StorageValidationResult(
                true,
                path,
                "Folder is available and writable.");
        }
        catch (Exception ex)
        {
            return new StorageValidationResult(
                false,
                path,
                ex.Message);
        }
    }
}
