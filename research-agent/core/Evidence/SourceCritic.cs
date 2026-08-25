using ResearchAgent.Core.Domain;

namespace ResearchAgent.Core.Evidence;

public sealed record SourceCritique(
    SourceOriginality Originality, InformantKnowledge InformantKnowledge,
    EvidenceClass EvidenceClass, decimal ProximityScore, decimal SuggestedWeight,
    string? IndependenceGroup, IReadOnlyList<string> Reasons);

public static class SourceCritic
{
    public static SourceCritique Review(
        SourceOriginality originality, InformantKnowledge informant,
        EvidenceClass evidenceClass, decimal proximityScore,
        string? independenceGroup, params string[] reasons)
    {
        var proximity = Math.Clamp(proximityScore, 0m, 100m);
        var baseWeight = 100m;
        baseWeight *= originality switch
        {
            SourceOriginality.Original => 1m,
            SourceOriginality.Derivative => .80m,
            SourceOriginality.AuthoredNarrative => .65m,
            _ => .55m
        };
        baseWeight *= informant switch
        {
            InformantKnowledge.Primary => 1m,
            InformantKnowledge.Secondary => .75m,
            _ => .60m
        };
        baseWeight *= evidenceClass switch
        {
            EvidenceClass.Direct => 1m,
            EvidenceClass.Indirect => .80m,
            EvidenceClass.Negative => .70m,
            _ => .60m
        };
        baseWeight *= proximity / 100m;
        return new(originality, informant, evidenceClass, proximity,
            Math.Clamp(baseWeight, 0m, 100m), independenceGroup, reasons);
    }

    public static EvidenceLink Apply(Guid evidenceId, Guid claimId, Guid citationId,
        EvidenceDirection direction, SourceCritique critique, string? notes = null) =>
        new(evidenceId, claimId, citationId, direction, critique.EvidenceClass,
            critique.Originality, critique.InformantKnowledge, critique.ProximityScore,
            critique.IndependenceGroup, critique.SuggestedWeight, notes);
}
