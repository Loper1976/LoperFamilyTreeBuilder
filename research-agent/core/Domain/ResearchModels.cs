using System.Security.Cryptography;

namespace ResearchAgent.Core.Domain;

public enum PrivacyClass { PublicHistorical, PrivateFamily, LivingPersonSensitive }
public enum ClaimStatus { Unverified, Possible, Probable, Verified, Conflicting, Rejected }
public enum EvidenceDirection { Supports, Conflicts, Neutral }
public enum EvidenceClass { Direct, Indirect, Negative }
public enum SourceOriginality { Original, Derivative, AuthoredNarrative, Unknown }
public enum InformantKnowledge { Primary, Secondary, Unknown }
public enum AiRole { Extractor, Researcher, Skeptic, SourceCritic, TimelineReviewer, Synthesizer }
public enum IdentityDecision { Unreviewed, SamePersonProposed, DifferentPersonProposed, Rejected }

public sealed record ResearchSource(
    Guid Id, string Title, string? SourceType, string? RepositoryName,
    Uri? OriginalUrl, DateTimeOffset RetrievedUtc, string OriginalFileName,
    string ArchivedRelativePath, string Sha256, string? MimeType, long FileSize,
    PrivacyClass PrivacyClass, bool IsOriginalImmutable = true, string? Notes = null);

public sealed record Citation(
    Guid Id, Guid SourceId, string? Page = null, string? ImageNumber = null,
    string? LineNumber = null, string? HouseholdNumber = null,
    string? EntryNumber = null, string? FieldName = null,
    string? LocatorText = null, string? QuotedOrTranscribedText = null,
    DateTimeOffset? CreatedUtc = null);

public sealed record ResearchClaim(
    Guid Id, Guid SubjectPersonId, string ClaimType, string ProposedValue,
    string? NormalizedValue, ClaimStatus Status, decimal Confidence,
    DateTimeOffset CreatedUtc, DateTimeOffset UpdatedUtc,
    Guid? AcceptedTreeFactId = null);

public sealed record EvidenceLink(
    Guid Id, Guid ClaimId, Guid CitationId, EvidenceDirection Direction,
    EvidenceClass EvidenceClass, SourceOriginality SourceOriginality,
    InformantKnowledge InformantKnowledge, decimal ProximityScore,
    string? IndependenceGroup, decimal Weight, string? Notes = null);

public sealed record CandidateIdentityMatch(
    Guid Id, Guid ExistingPersonId, string CandidateReference,
    decimal NameScore, decimal DateScore, decimal PlaceScore,
    decimal HouseholdScore, decimal RelativeScore, decimal OccupationScore,
    decimal AssociateScore, decimal NegativeEvidencePenalty,
    decimal TotalScore, string ExplanationJson, IdentityDecision Decision);

public sealed record AiAnalysis(
    Guid Id, Guid? RelatedClaimId, Guid? RelatedHypothesisId,
    string Provider, string Model, AiRole Role, string PromptHash,
    string ResponseText, string? StructuredResponseJson, decimal? Confidence,
    DateTimeOffset CreatedUtc, string PrivacyRoute);

public static class Hashing
{
    public static async Task<string> Sha256Async(Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        using var sha = SHA256.Create();
        var hash = await sha.ComputeHashAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
