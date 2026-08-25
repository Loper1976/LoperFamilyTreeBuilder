using ResearchAgent.Core.Integration;
using ResearchAgent.Core.Review;

namespace ResearchAgent.Core.Tests;

public sealed class HostIntegrationTests
{
    [Fact]
    public async Task PromotionRequiresVerifiedBackupFirst()
    {
        var claim = Guid.NewGuid();
        var approval = new ApprovalRecord(Guid.NewGuid(), "ResearchClaim", claim,
            ResearchDecision.AcceptForPromotionReview, "tester", "supported by reviewed evidence", DateTimeOffset.UtcNow);
        var backup = new FakeBackup();
        var promotion = new FakePromotion();
        var coordinator = new GuardedPromotionCoordinator(backup, promotion);
        await coordinator.PromoteAsync(claim, approval);
        Assert.Equal(new[] { "create", "verify", "promote" }, backup.Events.Concat(promotion.Events));
    }

    [Fact]
    public async Task WrongApprovalCannotPromote()
    {
        var claim = Guid.NewGuid();
        var approval = new ApprovalRecord(Guid.NewGuid(), "ResearchClaim", claim,
            ResearchDecision.NeedsMoreResearch, "tester", "not enough evidence", DateTimeOffset.UtcNow);
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new GuardedPromotionCoordinator(new FakeBackup(), new FakePromotion()).PromoteAsync(claim, approval));
    }

    private sealed class FakeBackup : IHostBackupGate
    {
        public List<string> Events { get; } = [];
        public Task<string> CreatePreChangeBackupAsync(string reason, CancellationToken cancellationToken = default) { Events.Add("create"); return Task.FromResult("backup"); }
        public Task VerifyBackupAsync(string backupReference, CancellationToken cancellationToken = default) { Events.Add("verify"); return Task.CompletedTask; }
    }
    private sealed class FakePromotion : IAcceptedTreePromotionService
    {
        public List<string> Events { get; } = [];
        public Task<Guid> PromoteClaimAsync(Guid researchClaimId, ApprovalRecord approval, CancellationToken cancellationToken = default) { Events.Add("promote"); return Task.FromResult(Guid.NewGuid()); }
    }
}
