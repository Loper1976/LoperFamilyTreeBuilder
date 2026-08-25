using System.Net.Http.Json;
using System.Text.Json;

namespace ResearchAgent.Core.AI;

public sealed class OllamaJsonClient : IStructuredModelClient
{
    private readonly HttpClient _http;
    public string Provider => "ollama";
    public string Model { get; }
    public ProviderTier Tier => ProviderTier.LocalFree;

    public OllamaJsonClient(HttpClient http, string model)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        Model = string.IsNullOrWhiteSpace(model) ? throw new ArgumentException("Ollama model is required.", nameof(model)) : model;
    }

    public async Task<string> CompleteJsonAsync(string systemPrompt, string userContent, CancellationToken cancellationToken = default)
    {
        var request = new
        {
            model = Model,
            stream = false,
            format = "json",
            options = new { temperature = 0 },
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userContent }
            }
        };
        using var response = await _http.PostAsJsonAsync("api/chat", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        if (!doc.RootElement.TryGetProperty("message", out var message) ||
            !message.TryGetProperty("content", out var content))
            throw new InvalidDataException("Ollama response did not contain message.content.");
        return content.GetString() ?? throw new InvalidDataException("Ollama returned empty content.");
    }
}
