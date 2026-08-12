namespace LoperFamilyTreeBuilder.Core.Entities;

public sealed class DnaSharedMatch
{
    private DnaSharedMatch()
    {
    }

    public DnaSharedMatch(Guid firstMatchId, Guid secondMatchId, string? evidenceSource = null)
    {
        if (firstMatchId == Guid.Empty)
            throw new ArgumentException("The first DNA match identifier cannot be empty.", nameof(firstMatchId));
        if (secondMatchId == Guid.Empty)
            throw new ArgumentException("The second DNA match identifier cannot be empty.", nameof(secondMatchId));
        if (firstMatchId == secondMatchId)
            throw new ArgumentException("A DNA match cannot be a shared-match link to itself.", nameof(secondMatchId));

        Id = Guid.NewGuid();
        if (firstMatchId.CompareTo(secondMatchId) < 0)
        {
            MatchAId = firstMatchId;
            MatchBId = secondMatchId;
        }
        else
        {
            MatchAId = secondMatchId;
            MatchBId = firstMatchId;
        }

        EvidenceSource = string.IsNullOrWhiteSpace(evidenceSource)
            ? "Provider shared-match evidence"
            : evidenceSource.Trim();
        CreatedUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid MatchAId { get; private set; }
    public DnaMatch? MatchA { get; private set; }
    public Guid MatchBId { get; private set; }
    public DnaMatch? MatchB { get; private set; }
    public string EvidenceSource { get; private set; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; private set; }
}
