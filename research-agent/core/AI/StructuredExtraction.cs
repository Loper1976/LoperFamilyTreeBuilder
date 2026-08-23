using System.Text.Json;
using ResearchAgent.Core.Domain;
using ResearchAgent.Core.Extraction;

namespace ResearchAgent.Core.AI;

public interface IStructuredModelClient
{
    string Provider { get; }
    string Model { get; }
    ProviderTier Tier { get; }
    Task<string> CompleteJsonAsync(string systemPrompt, string userContent, CancellationToken cancellationToken = default);
}

public sealed record ModelExtractionEnvelope(
    string DocumentType,
    List<ExtractedPerson>? People,
    List<ProposedFact>? ProposedFacts,
    List<string>? Warnings);

public sealed class StructuredModelExtractor(IStructuredModelClient client, RoutingPolicy policy) : IDocumentExtractor
{
    public string Provider => client.Provider;
    public string Model => client.Model;

    public async Task<DocumentExtractionResult> ExtractAsync(ResearchSource source, string archivedAbsolutePath,
        CancellationToken cancellationToken = default)
    {
        if (!PrivacyRouter.IsTierAllowed(source.PrivacyClass, client.Tier, policy))
            throw new InvalidOperationException($"Provider tier {client.Tier} is not permitted for {source.PrivacyClass} data.");

        var extension = Path.GetExtension(source.OriginalFileName).ToLowerInvariant();
        var isTextMime = source.MimeType?.StartsWith("text/", StringComparison.OrdinalIgnoreCase) == true;
        if (!isTextMime && extension is not ".txt" and not ".csv" and not ".json" and not ".ged")
            throw new NotSupportedException("This structured extractor currently accepts text-bearing inputs only. Vision/PDF adapters are separate providers.");

        var text = await File.ReadAllTextAsync(archivedAbsolutePath, cancellationToken);
        var prompt = """
You extract genealogy information from supplied source text. Return JSON only.
Never invent missing names, dates, places, relationships, page numbers, or facts.
Every proposed fact must include an evidenceLocator that points to text actually supplied.
Model confidence is extraction confidence only and has zero evidentiary weight.
Use this schema:
{"documentType":"string","people":[{"displayName":"string","relationship":null,"age":null,"birthPlace":null,"residence":null,"occupation":null,"otherFields":null}],"proposedFacts":[{"claimType":"string","proposedValue":"string","normalizedValue":null,"modelConfidence":0,"evidenceLocator":"string","supportingText":null}],"warnings":[]}
""";
        var json = await client.CompleteJsonAsync(prompt, text, cancellationToken);
        var envelope = JsonSerializer.Deserialize<ModelExtractionEnvelope>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidDataException("Model returned no structured extraction object.");

        Validate(envelope, text);
        return new DocumentExtractionResult(source.Id, envelope.DocumentType, text,
            envelope.People ?? [], envelope.ProposedFacts ?? [], envelope.Warnings ?? [],
            client.Provider, client.Model, DateTimeOffset.UtcNow);
    }

    private static void Validate(ModelExtractionEnvelope envelope, string sourceText)
    {
        if (string.IsNullOrWhiteSpace(envelope.DocumentType)) throw new InvalidDataException("documentType is required.");
        foreach (var fact in envelope.ProposedFacts ?? [])
        {
            if (string.IsNullOrWhiteSpace(fact.ClaimType) || string.IsNullOrWhiteSpace(fact.ProposedValue))
                throw new InvalidDataException("Every proposed fact requires claimType and proposedValue.");
            if (string.IsNullOrWhiteSpace(fact.EvidenceLocator))
                throw new InvalidDataException("Every proposed fact requires an evidenceLocator.");
            if (!string.IsNullOrWhiteSpace(fact.SupportingText) && !sourceText.Contains(fact.SupportingText, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("A proposed fact cited supporting text that is not present in the supplied source.");
        }
    }
}
