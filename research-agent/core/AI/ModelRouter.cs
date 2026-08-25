using ResearchAgent.Core.Domain;

namespace ResearchAgent.Core.AI;

public sealed record ModelCapability(
    string Provider, string Model, ProviderTier Tier,
    bool SupportsText, bool SupportsVision, bool SupportsStructuredOutput,
    decimal ExtractionScore, decimal IdentityScore, decimal ReasoningScore,
    bool IsAvailable = true);

public sealed record ModelTask(
    string TaskType, PrivacyClass PrivacyClass, bool RequiresVision = false,
    bool RequiresStructuredOutput = false, decimal MinimumQuality = 0m);

public sealed record ModelSelection(ModelCapability Model, decimal FitnessScore, string Reason);

public static class ModelRouter
{
    public static ModelSelection? Select(
        ModelTask task, IEnumerable<ModelCapability> models, RoutingPolicy policy)
    {
        var candidates = models
            .Where(m => m.IsAvailable)
            .Where(m => PrivacyRouter.IsTierAllowed(task.PrivacyClass, m.Tier, policy))
            .Where(m => !task.RequiresVision || m.SupportsVision)
            .Where(m => !task.RequiresStructuredOutput || m.SupportsStructuredOutput)
            .Select(m => new { Model = m, Score = Fitness(task, m) })
            .Where(x => x.Score >= task.MinimumQuality)
            .OrderBy(x => TierRank(x.Model.Tier))
            .ThenByDescending(x => x.Score)
            .FirstOrDefault();

        return candidates is null ? null : new ModelSelection(candidates.Model, candidates.Score,
            $"Selected cheapest allowed tier meeting task requirements; benchmark fitness={candidates.Score:0.0}.");
    }

    private static decimal Fitness(ModelTask task, ModelCapability m) => task.TaskType.ToLowerInvariant() switch
    {
        "extraction" or "classification" => m.ExtractionScore,
        "identity" or "record-match" => m.IdentityScore,
        "skeptic" or "source-criticism" or "synthesis" => m.ReasoningScore,
        _ => (m.ExtractionScore + m.IdentityScore + m.ReasoningScore) / 3m
    };

    private static int TierRank(ProviderTier tier) => tier switch
    {
        ProviderTier.Deterministic => 0,
        ProviderTier.LocalFree => 1,
        ProviderTier.FreeCloud => 2,
        ProviderTier.PaidLowCost => 3,
        ProviderTier.PaidStrong => 4,
        _ => 99
    };
}
