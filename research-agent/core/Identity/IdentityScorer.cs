namespace ResearchAgent.Core.Identity;

public sealed record IdentitySignals(
    decimal Name, decimal Date, decimal Place, decimal Household,
    decimal Relatives, decimal Occupation, decimal Associates,
    decimal NegativeEvidencePenalty);

public sealed record IdentityAssessment(decimal Score, string Recommendation, IReadOnlyDictionary<string, decimal> Components);

public static class IdentityScorer
{
    public static IdentityAssessment Assess(IdentitySignals s)
    {
        var components = new Dictionary<string, decimal>
        {
            ["name"] = Clamp(s.Name) * .24m,
            ["date"] = Clamp(s.Date) * .16m,
            ["place"] = Clamp(s.Place) * .14m,
            ["household"] = Clamp(s.Household) * .14m,
            ["relatives"] = Clamp(s.Relatives) * .14m,
            ["occupation"] = Clamp(s.Occupation) * .07m,
            ["associates"] = Clamp(s.Associates) * .11m,
            ["negativeEvidencePenalty"] = -Clamp(s.NegativeEvidencePenalty)
        };

        var score = Math.Clamp(components.Values.Sum(), 0m, 100m);
        var recommendation = score switch
        {
            >= 85m => "Strong same-person candidate; human/evidence review still required.",
            >= 65m => "Probable candidate; obtain corroborating evidence.",
            >= 40m => "Possible candidate; identity remains unresolved.",
            _ => "Weak candidate or materially contradicted identity."
        };

        return new(score, recommendation, components);
    }

    private static decimal Clamp(decimal value) => Math.Clamp(value, 0m, 100m);
}
