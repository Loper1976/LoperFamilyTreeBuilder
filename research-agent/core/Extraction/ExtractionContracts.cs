using ResearchAgent.Core.Domain;

namespace ResearchAgent.Core.Extraction;

public sealed record ExtractedPerson(
    string DisplayName, string? Relationship, string? Age, string? BirthPlace,
    string? Residence, string? Occupation, IReadOnlyDictionary<string, string>? OtherFields = null);

public sealed record ProposedFact(
    string ClaimType, string ProposedValue, string? NormalizedValue,
    decimal ModelConfidence, string EvidenceLocator, string? SupportingText = null);

public sealed record DocumentExtractionResult(
    Guid SourceId, string DocumentType, string? Transcription,
    IReadOnlyList<ExtractedPerson> People, IReadOnlyList<ProposedFact> ProposedFacts,
    IReadOnlyList<string> Warnings, string Provider, string Model, DateTimeOffset CreatedUtc);

public interface IDocumentExtractor
{
    string Provider { get; }
    string Model { get; }
    Task<DocumentExtractionResult> ExtractAsync(ResearchSource source, string archivedAbsolutePath,
        CancellationToken cancellationToken = default);
}

public static class ProposalFactory
{
    public static IReadOnlyList<ResearchClaim> ToResearchClaims(Guid personId, DocumentExtractionResult result)
    {
        return result.ProposedFacts.Select(f => new ResearchClaim(
            Guid.NewGuid(), personId, f.ClaimType, f.ProposedValue, f.NormalizedValue,
            ClaimStatus.Unverified, Math.Clamp(f.ModelConfidence, 0m, 100m),
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow)).ToArray();
    }
}
