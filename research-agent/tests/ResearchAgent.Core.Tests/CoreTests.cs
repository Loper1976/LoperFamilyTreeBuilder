using ResearchAgent.Core.AI;
using ResearchAgent.Core.Domain;
using ResearchAgent.Core.Evidence;
using ResearchAgent.Core.Identity;

namespace ResearchAgent.Core.Tests;

public sealed class CoreTests
{
    [Fact]
    public void AiVoteIsNotPartOfEvidenceAssessment()
    {
        var result = EvidenceScorer.Assess([]);
        Assert.Equal(ClaimStatus.Unverified, result.Status);
        Assert.Equal(0m, result.Score);
    }

    [Fact]
    public void MaterialConflictForcesConflictingStatus()
    {
        var claim = Guid.NewGuid();
        var citation = Guid.NewGuid();
        var links = new[]
        {
            new EvidenceLink(Guid.NewGuid(), claim, citation, EvidenceDirection.Supports, EvidenceClass.Direct,
                SourceOriginality.Original, InformantKnowledge.Primary, 100, "A", 90),
            new EvidenceLink(Guid.NewGuid(), claim, citation, EvidenceDirection.Conflicts, EvidenceClass.Direct,
                SourceOriginality.Original, InformantKnowledge.Primary, 100, "B", 70)
        };
        Assert.Equal(ClaimStatus.Conflicting, EvidenceScorer.Assess(links).Status);
    }

    [Fact]
    public void IdentityScoreNeverAuthorizesMerge()
    {
        var result = IdentityScorer.Assess(new(100, 100, 100, 100, 100, 100, 100, 0));
        Assert.True(result.Score >= 85);
        Assert.Contains("review", result.Recommendation, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SensitiveDataDefaultsToLocalOnly()
    {
        var tiers = PrivacyRouter.PreferredTiers(PrivacyClass.LivingPersonSensitive, new RoutingPolicy());
        Assert.Contains(ProviderTier.Deterministic, tiers);
        Assert.Contains(ProviderTier.LocalFree, tiers);
        Assert.DoesNotContain(ProviderTier.FreeCloud, tiers);
        Assert.DoesNotContain(ProviderTier.PaidStrong, tiers);
    }
}
