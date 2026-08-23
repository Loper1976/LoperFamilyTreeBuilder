using ResearchAgent.Core.Domain;

namespace ResearchAgent.Core.AI;

public enum ProviderTier { Deterministic, LocalFree, FreeCloud, PaidLowCost, PaidStrong }

public sealed record RoutingPolicy(
    bool AllowPublicHistoricalCloudAi = false,
    bool AllowPrivateFamilyCloudAi = false,
    bool AllowLivingPersonCloudAi = false);

public static class PrivacyRouter
{
    public static bool IsTierAllowed(PrivacyClass privacy, ProviderTier tier, RoutingPolicy policy)
    {
        if (tier is ProviderTier.Deterministic or ProviderTier.LocalFree) return true;

        return privacy switch
        {
            PrivacyClass.PublicHistorical => policy.AllowPublicHistoricalCloudAi,
            PrivacyClass.PrivateFamily => policy.AllowPrivateFamilyCloudAi,
            PrivacyClass.LivingPersonSensitive => policy.AllowLivingPersonCloudAi,
            _ => false
        };
    }

    public static IReadOnlyList<ProviderTier> PreferredTiers(PrivacyClass privacy, RoutingPolicy policy)
    {
        var order = new[]
        {
            ProviderTier.Deterministic,
            ProviderTier.LocalFree,
            ProviderTier.FreeCloud,
            ProviderTier.PaidLowCost,
            ProviderTier.PaidStrong
        };
        return order.Where(t => IsTierAllowed(privacy, t, policy)).ToArray();
    }
}
