using System.Net.Http.Json;
using System.Text.Json;

namespace ResearchAgent.Core.Research;

// Generic adapter for permitted public search APIs returning JSON.
// It does not scrape HTML and does not bypass authentication or access controls.
public sealed class PublicWebSearchProvider : ISourceSearchProvider
{
    private readonly HttpClient _http;
    private readonly Func<SourceSearchQuery, Uri> _queryBuilder;
    private readonly Func<JsonElement, IEnumerable<SourceSearchResult>> _parser;
    public string Name { get; }

    public PublicWebSearchProvider(string name, HttpClient http,
        Func<SourceSearchQuery, Uri> queryBuilder,
        Func<JsonElement, IEnumerable<SourceSearchResult>> parser)
    {
        Name = name;
        _http = http;
        _queryBuilder = queryBuilder;
        _parser = parser;
    }

    public bool Supports(SourceSearchQuery query) => query.PrivacyClass == Domain.PrivacyClass.PublicHistorical;

    public async Task<IReadOnlyList<SourceSearchResult>> SearchAsync(SourceSearchQuery query, CancellationToken cancellationToken = default)
    {
        var uri = _queryBuilder(query);
        using var response = await _http.GetAsync(uri, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        return _parser(doc.RootElement).ToArray();
    }
}
