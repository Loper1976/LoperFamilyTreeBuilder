using ResearchAgent.Core.Domain;

namespace ResearchAgent.Core.Evidence;

public sealed record EvidenceAssessment(ClaimStatus Status, decimal Score, IReadOnlyList<string> Reasons);

public static class EvidenceScorer
{
    public static EvidenceAssessment Assess(IEnumerable<EvidenceLink> links)
    {
        var items = links?.ToList() ?? throw new ArgumentNullException(nameof(links));
        if (items.Count == 0)
            return new(ClaimStatus.Unverified, 0m, ["No documentary evidence is linked to the claim."]);

        decimal support = 0m;
        decimal conflict = 0m;
        var reasons = new List<string>();
        var supportGroups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in items)
        {
            var quality = Clamp(item.Weight, 0m, 100m);
            quality *= OriginalityFactor(item.SourceOriginality);
            quality *= InformantFactor(item.InformantKnowledge);
            quality *= EvidenceFactor(item.EvidenceClass);
            quality *= Clamp(item.ProximityScore, 0m, 100m) / 100m;

            if (item.Direction == EvidenceDirection.Supports)
            {
                support += quality;
                if (!string.IsNullOrWhiteSpace(item.IndependenceGroup)) supportGroups.Add(item.IndependenceGroup);
            }
            else if (item.Direction == EvidenceDirection.Conflicts)
            {
                conflict += quality;
            }
        }

        if (conflict >= 40m)
        {
            reasons.Add($"Material conflicting evidence score: {conflict:0.0}.");
            return new(ClaimStatus.Conflicting, Clamp(support - conflict, 0m, 100m), reasons);
        }

        if (support >= 120m && supportGroups.Count >= 2)
        {
            reasons.Add("At least two identified independent evidence groups support the claim.");
            return new(ClaimStatus.Verified, Clamp(support / Math.Max(2, supportGroups.Count), 0m, 100m), reasons);
        }

        if (support >= 65m)
        {
            reasons.Add("Documentary support is substantial but does not yet meet the verified threshold.");
            return new(ClaimStatus.Probable, Clamp(support, 0m, 95m), reasons);
        }

        if (support > 0m)
        {
            reasons.Add("Some documentary support exists; more corroboration is required.");
            return new(ClaimStatus.Possible, Clamp(support, 0m, 75m), reasons);
        }

        return new(ClaimStatus.Unverified, 0m, ["No supporting documentary evidence remains after scoring."]);
    }

    private static decimal OriginalityFactor(SourceOriginality value) => value switch
    {
        SourceOriginality.Original => 1.00m,
        SourceOriginality.Derivative => 0.80m,
        SourceOriginality.AuthoredNarrative => 0.65m,
        _ => 0.60m
    };

    private static decimal InformantFactor(InformantKnowledge value) => value switch
    {
        InformantKnowledge.Primary => 1.00m,
        InformantKnowledge.Secondary => 0.75m,
        _ => 0.65m
    };

    private static decimal EvidenceFactor(EvidenceClass value) => value switch
    {
        EvidenceClass.Direct => 1.00m,
        EvidenceClass.Indirect => 0.80m,
        EvidenceClass.Negative => 0.70m,
        _ => 0.60m
    };

    private static decimal Clamp(decimal value, decimal min, decimal max) => Math.Min(max, Math.Max(min, value));
}
