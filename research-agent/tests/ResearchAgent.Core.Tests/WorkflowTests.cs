using ResearchAgent.Core.AI;
using ResearchAgent.Core.Domain;
using ResearchAgent.Core.Proof;
using ResearchAgent.Core.Research;
using ResearchAgent.Core.Timeline;

namespace ResearchAgent.Core.Tests;

public sealed class WorkflowTests
{
    [Fact]
    public void RouterPrefersLocalFreeWhenQualityIsEnough()
    {
        var models = new[]
        {
            new ModelCapability("local", "small", ProviderTier.LocalFree, true, false, true, 85, 70, 65),
            new ModelCapability("paid", "strong", ProviderTier.PaidStrong, true, true, true, 99, 99, 99)
        };
        var selected = ModelRouter.Select(new("extraction", PrivacyClass.PublicHistorical, MinimumQuality: 80),
            models, new RoutingPolicy(AllowPublicHistoricalCloudAi: true));
        Assert.NotNull(selected);
        Assert.Equal(ProviderTier.LocalFree, selected!.Model.Tier);
    }

    [Fact]
    public void TimelineFlagsEventAfterDeath()
    {
        var p = Guid.NewGuid();
        var events = new[]
        {
            new LifeEvent(Guid.NewGuid(), p, LifeEventType.Birth, new DateOnly(1860, 1, 1), null, "Texas"),
            new LifeEvent(Guid.NewGuid(), p, LifeEventType.Death, new DateOnly(1920, 1, 1), null, "Texas"),
            new LifeEvent(Guid.NewGuid(), p, LifeEventType.Census, new DateOnly(1930, 4, 1), null, "Texas")
        };
        Assert.Contains(TimelineConsistency.Check(events), w => w.Code == "EVENT_AFTER_DEATH");
    }

    [Fact]
    public void ResearchRankingFavorsHigherInformationValue()
    {
        var now = DateTimeOffset.UtcNow;
        var low = new ResearchTask(Guid.NewGuid(), null, null, "Low", "search", 40, 20, 30, 90, 0, ResearchTaskStatus.Open, now);
        var high = new ResearchTask(Guid.NewGuid(), null, null, "High", "search", 90, 95, 90, 80, 10, ResearchTaskStatus.Open, now);
        Assert.Equal("High", ResearchPriorityEngine.Rank([low, high])[0].Task.Question);
    }

    [Fact]
    public void ProofPacketExplicitlyGivesAiZeroEvidenceWeight()
    {
        var person = Guid.NewGuid();
        var claim = new ResearchClaim(Guid.NewGuid(), person, "parent", "Example Parent", null,
            ClaimStatus.Unverified, 0, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var ai = new AiAnalysis(Guid.NewGuid(), claim.Id, null, "test", "model", AiRole.Researcher,
            "hash", "Looks plausible", null, 99, DateTimeOffset.UtcNow, "local");
        var packet = ProofPacketBuilder.Build(new(person, "Who is the parent?", claim, [], [], [], [ai], ["Find a primary record"]));
        Assert.Contains("\"evidentiaryWeight\": 0", packet.PacketJson);
        Assert.Equal(ClaimStatus.Unverified, packet.FinalStatus);
    }
}
