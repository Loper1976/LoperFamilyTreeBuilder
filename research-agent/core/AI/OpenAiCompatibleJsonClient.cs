using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace ResearchAgent.Core.AI;

public sealed class OpenAiCompatibleJsonClient : IStructuredModelClient
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    public string Provider { get; }
    public string Model { get; }
    public ProviderTier Tier { get; }

    public OpenAiCompatibleJsonClient(HttpClient http, string provider, string model, string apiKey,
        ProviderTier tier = ProviderTier.FreeCloud)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        Provider = string.IsNullOrWhiteSpace(provider) ? throw new ArgumentException("Provider required.") : provider;
        Model = string.IsNullOrWhiteSpace(model) ? throw new ArgumentException("Model required.") : model;
        _apiKey = string.IsNullOrWhiteSpace(apiKey) ? throw new ArgumentException("API key required.") : apiKey;
        Tier = tier;
    }

    public async Task<string> CompleteJsonAsync(string systemPrompt, string userContent, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        request.Content = JsonContent.Create(new
        {
            model = Model,
            temperature = 0,
            response_format = new { type = "json_object" },
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userContent }
            }
        });
        using var response = await _http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()
            ?? throw new InvalidDataException("Provider returned empty content.");
    }
}
