using ResearchAgent.Core.Domain;
using ResearchAgent.Core.Persistence;
using ResearchAgent.Core.Proof;
using ResearchAgent.Core.Research;
using ResearchAgent.Core.Review;

namespace ResearchAgent.Core.Tests;

public sealed class DecisionTests
{
    [Fact]
    public async Task CannotAcceptClaimWithoutReviewedSupportingEvidence()
    {
        var claim = new ResearchClaim(Guid.NewGuid(), Guid.NewGuid(), "father", "Fictional Parent", null,
            ClaimStatus.Unverified, 99, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var store = new FakeStore();
        var approvals = new FakeApprovalStore();
        var service = new ResearchDecisionService(store, approvals);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DecideClaimAsync(claim,
            ResearchDecision.AcceptForPromotionReview, "tester", "test"));
    }

    private sealed class FakeApprovalStore : IApprovalStore
    {
        public Task SaveApprovalAsync(ApprovalRecord record, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class FakeStore : IResearchStore
    {
        public Task<IReadOnlyList<EvidenceLink>> GetEvidenceForClaimAsync(Guid claimId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<EvidenceLink>>([]);
        public Task<ResearchSource?> GetSourceAsync(Guid id, CancellationToken ct = default) => Task.FromResult<ResearchSource?>(null);
        public Task<ResearchClaim?> GetClaimAsync(Guid id, CancellationToken ct = default) => Task.FromResult<ResearchClaim?>(null);
        public Task<IReadOnlyList<ResearchTask>> GetOpenTasksAsync(Guid? personId = null, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<ResearchTask>>([]);
        public Task SaveSourceAsync(ResearchSource x, CancellationToken ct = default) => Task.CompletedTask;
        public Task SaveCitationAsync(Citation x, CancellationToken ct = default) => Task.CompletedTask;
        public Task SaveClaimAsync(ResearchClaim x, CancellationToken ct = default) => Task.CompletedTask;
        public Task SaveEvidenceAsync(EvidenceLink x, CancellationToken ct = default) => Task.CompletedTask;
        public Task SaveAiAnalysisAsync(AiAnalysis x, CancellationToken ct = default) => Task.CompletedTask;
        public Task SaveResearchTaskAsync(ResearchTask x, CancellationToken ct = default) => Task.CompletedTask;
        public Task SaveProofPacketAsync(ProofPacket x, CancellationToken ct = default) => Task.CompletedTask;
    }
}
