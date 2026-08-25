using System.Text.Json;
using ResearchAgent.Core.Domain;

namespace ResearchAgent.Core.Research;

public sealed class NaraCatalogSearchProvider(HttpClient http) : ISourceSearchProvider
{
    private const string SearchEndpoint =
        "https://catalog.archives.gov/proxy/records/search";

    public string Name => "National Archives Catalog";

    public bool Supports(SourceSearchQuery query) =>
        query.PrivacyClass == PrivacyClass.PublicHistorical;

    public async Task<IReadOnlyList<SourceSearchResult>> SearchAsync(
        SourceSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        var searchText = string.Join(' ', new[] { query.QueryText, query.Place }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
        var uri = new Uri(
            $"{SearchEndpoint}?q={Uri.EscapeDataString(searchText)}&limit=10&abbreviated=true");

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
        if (!root.TryGetProperty("body", out var body) ||
            !body.TryGetProperty("hits", out var hits) ||
            !hits.TryGetProperty("hits", out var rows) ||
            rows.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var results = new List<SourceSearchResult>();
        foreach (var row in rows.EnumerateArray())
        {
            if (!row.TryGetProperty("_source", out var source) ||
                !source.TryGetProperty("record", out var record))
            {
                continue;
            }

            var title = GetString(record, "title");
            var naId = GetString(record, "naId");
            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(naId))
                continue;

            var level = GetString(record, "levelOfDescription");
            var materials = GetFirstString(record, "generalRecordsTypes");
            var metadata = new Dictionary<string, string>
            {
                ["NaraId"] = naId,
                ["LevelOfDescription"] = level ?? string.Empty
            };

            results.Add(new SourceSearchResult(
                "National Archives Catalog",
                title,
                new Uri($"https://catalog.archives.gov/id/{naId}"),
                "U.S. National Archives and Records Administration",
                materials ?? level ?? requestedRecordType,
                BuildSummary(record, level, materials),
                Math.Max(50m, 85m - results.Count * 3m),
                RequiresAuthentication: false,
                CanArchiveOriginal: false,
                metadata));
        }

        return results;
    }

    private static string? GetString(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value)) return null;
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            _ => null
        };
    }

    private static string? GetFirstString(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var values) ||
            values.ValueKind != JsonValueKind.Array)
            return null;
        foreach (var value in values.EnumerateArray())
            if (value.ValueKind == JsonValueKind.String) return value.GetString();
        return null;
    }

    private static string BuildSummary(
        JsonElement record,
        string? level,
        string? materials)
    {
        var start = record.TryGetProperty("inclusiveStartDate", out var startDate)
            ? GetString(startDate, "logicalDate") ?? GetString(startDate, "year")
            : null;
        var end = record.TryGetProperty("inclusiveEndDate", out var endDate)
            ? GetString(endDate, "logicalDate") ?? GetString(endDate, "year")
            : null;
        return string.Join(" · ", new[]
            {
                level,
                materials,
                start is null ? null : end is null ? start : $"{start}–{end}"
            }
            .Where(value => !string.IsNullOrWhiteSpace(value)));
    }
}
