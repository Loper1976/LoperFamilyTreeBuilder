using System.Text.Json;

namespace LoperFamilyTreeBuilder.Infrastructure.Configuration;

public sealed class AiConfiguration
{
    public string OllamaEndpoint { get; set; } = "http://127.0.0.1:11434/";
    public string OllamaModel { get; set; } = "qwen2.5:3b";
}

public sealed class AiConfigurationStore(ApplicationPaths paths)
{
    private string FilePath => Path.Combine(paths.ConfigurationDirectory, "ai-settings.json");
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public async Task<AiConfiguration> LoadAsync(CancellationToken cancellationToken = default)
    {
        paths.EnsureLocalDirectories();
        if (!File.Exists(FilePath)) return new AiConfiguration();
        await using var stream = File.OpenRead(FilePath);
        return await JsonSerializer.DeserializeAsync<AiConfiguration>(stream, Options, cancellationToken)
            ?? new AiConfiguration();
    }

    public async Task SaveAsync(AiConfiguration configuration, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(configuration.OllamaEndpoint, UriKind.Absolute, out var endpoint) ||
            endpoint.Scheme is not ("http" or "https"))
            throw new InvalidOperationException("A valid Ollama HTTP endpoint is required.");
        if (string.IsNullOrWhiteSpace(configuration.OllamaModel))
            throw new InvalidOperationException("An Ollama model is required.");
        paths.EnsureLocalDirectories();
        var temporary = FilePath + ".tmp";
        await File.WriteAllTextAsync(temporary, JsonSerializer.Serialize(configuration, Options), cancellationToken);
        File.Move(temporary, FilePath, true);
    }
}
