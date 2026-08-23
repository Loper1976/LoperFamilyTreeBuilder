using System.Text.Json;

namespace LoperFamilyTreeBuilder.Infrastructure.Configuration;

public sealed class ArchiveConfigurationStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly ApplicationPaths _paths;

    public ArchiveConfigurationStore(ApplicationPaths paths)
    {
        _paths = paths;
    }

    public async Task<ArchiveConfiguration?> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        _paths.EnsureLocalDirectories();

        if (!File.Exists(_paths.ConfigurationFile))
        {
            return null;
        }

        await using var stream = File.OpenRead(_paths.ConfigurationFile);

        return await JsonSerializer.DeserializeAsync<ArchiveConfiguration>(
            stream,
            JsonOptions,
            cancellationToken);
    }

    public async Task SaveAsync(
        ArchiveConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        if (!configuration.IsComplete)
        {
            throw new InvalidOperationException(
                "Primary archive and backup locations are both required.");
        }

        _paths.EnsureLocalDirectories();

        var temporaryFile = _paths.ConfigurationFile + ".tmp";

        await using (var stream = File.Create(temporaryFile))
        {
            await JsonSerializer.SerializeAsync(
                stream,
                configuration,
                JsonOptions,
                cancellationToken);

            await stream.FlushAsync(cancellationToken);
        }

        File.Move(
            temporaryFile,
            _paths.ConfigurationFile,
            overwrite: true);
    }
}
