using ResearchAgent.Core.Domain;

namespace ResearchAgent.Core.Extraction;

public sealed record ProposedClaimWithCitation(ResearchClaim Claim, Citation Citation, EvidenceLink Evidence);

public static class CitationProposalService
{
    public static IReadOnlyList<ProposedClaimWithCitation> Build(
        Guid personId, ResearchSource source, DocumentExtractionResult extraction)
    {
        var results = new List<ProposedClaimWithCitation>();
        foreach (var fact in extraction.ProposedFacts)
        {
            var claim = new ResearchClaim(Guid.NewGuid(), personId, fact.ClaimType, fact.ProposedValue,
                fact.NormalizedValue, ClaimStatus.Unverified, Math.Clamp(fact.ModelConfidence, 0m, 100m),
                DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

            var citation = new Citation(Guid.NewGuid(), source.Id,
                LocatorText: fact.EvidenceLocator,
                QuotedOrTranscribedText: fact.SupportingText,
                CreatedUtc: DateTimeOffset.UtcNow);

            // Extraction confidence does not become evidence weight. Evidence classification
            // is intentionally unknown/neutral until a source critic or human reviews it.
            var evidence = new EvidenceLink(Guid.NewGuid(), claim.Id, citation.Id,
                EvidenceDirection.Neutral, EvidenceClass.Indirect, SourceOriginality.Unknown,
                InformantKnowledge.Unknown, 0m, null, 0m,
                "Auto-generated provenance link. Requires evidence review before scoring.");
            results.Add(new(claim, citation, evidence));
        }
        return results;
    }
}
