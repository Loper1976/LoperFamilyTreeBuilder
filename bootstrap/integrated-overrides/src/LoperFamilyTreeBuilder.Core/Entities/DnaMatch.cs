namespace LoperFamilyTreeBuilder.Core.Entities;

public sealed class DnaMatch
{
    private DnaMatch()
    {
    }

    public DnaMatch(
        string providerName,
        string externalMatchId,
        string displayName,
        decimal totalCentimorgans,
        int? sharedSegments = null)
    {
        if (string.IsNullOrWhiteSpace(providerName))
            throw new ArgumentException("A DNA provider name is required.", nameof(providerName));
        if (string.IsNullOrWhiteSpace(externalMatchId))
            throw new ArgumentException("A provider match identifier is required.", nameof(externalMatchId));
        if (string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("A match display name is required.", nameof(displayName));
        if (totalCentimorgans <= 0m || totalCentimorgans > 4000m)
            throw new ArgumentOutOfRangeException(nameof(totalCentimorgans), "Total shared DNA must be greater than 0 and no more than 4000 cM.");
        if (sharedSegments is < 0 or > 200)
            throw new ArgumentOutOfRangeException(nameof(sharedSegments), "Shared segment count must be between 0 and 200 when known.");

        Id = Guid.NewGuid();
        ProviderName = providerName.Trim();
        ExternalMatchId = externalMatchId.Trim();
        DisplayName = displayName.Trim();
        TotalCentimorgans = totalCentimorgans;
        SharedSegments = sharedSegments;
        Visibility = DnaMatchVisibility.OwnerOnly;
        ReviewStatus = DnaMatchReviewStatus.Imported;
        CreatedUtc = DateTimeOffset.UtcNow;
        ModifiedUtc = CreatedUtc;
    }

    public Guid Id { get; private set; }
    public string ProviderName { get; private set; } = string.Empty;
    public string ExternalMatchId { get; private set; } = string.Empty;
    public string DisplayName { get; private set; } = string.Empty;
    public decimal TotalCentimorgans { get; private set; }
    public int? SharedSegments { get; private set; }
    public DnaMatchVisibility Visibility { get; private set; }
    public DnaMatchReviewStatus ReviewStatus { get; private set; }
    public string ManualAncestralLineLabel { get; private set; } = string.Empty;
    public string ResearchNotes { get; private set; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; private set; }
    public DateTimeOffset ModifiedUtc { get; private set; }

    public void SetVisibility(DnaMatchVisibility visibility)
    {
        Visibility = visibility;
        Touch();
    }

    public void SaveResearchReview(string? manualAncestralLineLabel, string? researchNotes)
    {
        ManualAncestralLineLabel = (manualAncestralLineLabel ?? string.Empty).Trim();
        ResearchNotes = (researchNotes ?? string.Empty).Trim();
        ReviewStatus = DnaMatchReviewStatus.Reviewed;
        Touch();
    }

    private void Touch() => ModifiedUtc = DateTimeOffset.UtcNow;
}
