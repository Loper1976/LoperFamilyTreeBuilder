using System.Text.RegularExpressions;
using ResearchAgent.Core.Domain;

namespace ResearchAgent.Core.Extraction;

public sealed partial class TextDocumentExtractor : IDocumentExtractor
{
    public string Provider => "deterministic";
    public string Model => "none";

    public async Task<DocumentExtractionResult> ExtractAsync(ResearchSource source, string archivedAbsolutePath,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        var extension = Path.GetExtension(archivedAbsolutePath).ToLowerInvariant();
        if (extension is not ".txt" and not ".csv" and not ".json" and not ".ged")
            throw new NotSupportedException($"Deterministic text extraction does not support '{extension}'.");

        var text = await File.ReadAllTextAsync(archivedAbsolutePath, cancellationToken);
        var warnings = new List<string>();
        if (string.IsNullOrWhiteSpace(text)) warnings.Add("Document contains no readable text.");

        // This extractor intentionally does not infer genealogy facts. It only returns text.
        // Structured facts are proposed by a later parser/model and remain unverified.
        return new DocumentExtractionResult(source.Id, Classify(extension, text), text, [], [], warnings,
            Provider, Model, DateTimeOffset.UtcNow);
    }

    private static string Classify(string extension, string text)
    {
        if (extension == ".ged" || GedcomHeader().IsMatch(text)) return "GEDCOM";
        if (extension == ".csv") return "CSV";
        if (extension == ".json") return "JSON";
        return "Text";
    }

    [GeneratedRegex(@"(?m)^0\s+HEAD\s*$", RegexOptions.IgnoreCase)]
    private static partial Regex GedcomHeader();
}
