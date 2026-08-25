using ResearchAgent.Core.AI;
using ResearchAgent.Core.Domain;
using ResearchAgent.Core.Evidence;

namespace ResearchAgent.Core.Tests;

public sealed class EvidenceReviewTests
{
    [Fact]
    public void OriginalPrimaryDirectEvidenceScoresHigherThanDerivativeSecondary()
    {
        var strong = SourceCritic.Review(SourceOriginality.Original, InformantKnowledge.Primary, EvidenceClass.Direct, 100, "A");
        var weak = SourceCritic.Review(SourceOriginality.Derivative, InformantKnowledge.Secondary, EvidenceClass.Indirect, 70, "B");
        Assert.True(strong.SuggestedWeight > weak.SuggestedWeight);
    }

    [Fact]
    public void CompetingParentClaimsAreHighSeverityConflict()
    {
        var person = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var claims = new[]
        {
            new ResearchClaim(Guid.NewGuid(), person, "father", "John Example", null, ClaimStatus.Probable, 70, now, now),
            new ResearchClaim(Guid.NewGuid(), person, "father", "James Example", null, ClaimStatus.Possible, 50, now, now)
        };
        var conflict = ConflictDetector.Detect(claims).Single();
        Assert.Equal("High", conflict.Severity);
    }

    [Fact]
    public void ProviderBenchmarkPenalizesHallucination()
    {
        var registry = new ProviderRegistry();
        registry.RecordBenchmark(new("local", "model", "extraction", 95, 20, 100, 80, DateTimeOffset.UtcNow));
        Assert.Equal(75m, registry.EffectiveBenchmark("local", "model", "extraction"));
    }
}
