using System.Text.Json;
using ResearchAgent.Core.Domain;

namespace ResearchAgent.Core.Research;

public sealed class LibraryOfCongressSearchProvider(HttpClient http) : ISourceSearchProvider
{
    private const string SearchEndpoint = "https://www.loc.gov/search/";

    public string Name => "Library of Congress";

    public bool Supports(SourceSearchQuery query) =>
        query.PrivacyClass == PrivacyClass.PublicHistorical;

    public async Task<IReadOnlyList<SourceSearchResult>> SearchAsync(
        SourceSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        var searchText = string.Join(' ', new[] { query.QueryText, query.Place }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
        var uri = new Uri(
            $"{SearchEndpoint}?q={Uri.EscapeDataString(searchText)}&fo=json&c=10");

        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        request.Headers.Accept.ParseAdd("application/json");
        using var response = await http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(
            stream,
            cancellationToken: cancellationToken);
        return Parse(document.RootElement, query.RecordType);
    }

    internal static IReadOnlyList<SourceSearchResult> Parse(
        JsonElement root,
        string requestedRecordType)
    {
        if (!root.TryGetProperty("results", out var rows) ||
            rows.ValueKind != JsonValueKind.Array)
            return [];

        var results = new List<SourceSearchResult>();
        foreach (var row in rows.EnumerateArray())
        {
            var title = GetString(row, "title");
            var id = GetString(row, "id");
            if (string.IsNullOrWhiteSpace(title) ||
                !Uri.TryCreate(id, UriKind.Absolute, out var url))
                continue;

            if (url.Scheme == Uri.UriSchemeHttp)
                url = new UriBuilder(url) { Scheme = Uri.UriSchemeHttps, Port = -1 }.Uri;

            var format = GetFirstString(row, "original_format") ?? requestedRecordType;
            var description = GetFirstString(row, "description");
            var date = GetString(row, "date");
            var summary = string.Join(" · ", new[] { date, description }
                .Where(value => !string.IsNullOrWhiteSpace(value)));

            results.Add(new SourceSearchResult(
                "Library of Congress",
                title,
                url,
                "Library of Congress",
                format,
                summary,
                Math.Max(50m, 85m - results.Count * 3m),
                RequiresAuthentication: false,
                CanArchiveOriginal: false,
                new Dictionary<string, string> { ["LocItemUrl"] = url.ToString() }));
        }

        return results;
    }

    private static string? GetString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static string? GetFirstString(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var values) ||
            values.ValueKind != JsonValueKind.Array)
            return null;
        foreach (var value in values.EnumerateArray())
            if (value.ValueKind == JsonValueKind.String) return value.GetString();
        return null;
    }
}
