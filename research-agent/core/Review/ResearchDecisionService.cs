using ResearchAgent.Core.Domain;
using ResearchAgent.Core.Persistence;
using ResearchAgent.Core.Research;

namespace ResearchAgent.Core.Review;

public enum ResearchDecision { AcceptForPromotionReview, Reject, NeedsMoreResearch }
public sealed record ApprovalRecord(Guid Id, string EntityType, Guid EntityId, ResearchDecision Decision,
    string Actor, string Reason, DateTimeOffset CreatedUtc);

public interface IApprovalStore
{
    Task SaveApprovalAsync(ApprovalRecord record, CancellationToken cancellationToken = default);
}

public sealed class ResearchDecisionService(IResearchStore researchStore, IApprovalStore approvalStore)
{
    public async Task<ApprovalRecord> DecideClaimAsync(ResearchClaim claim, ResearchDecision decision,
        string actor, string reason, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(actor)) throw new ArgumentException("Actor is required.", nameof(actor));
        if (string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("A decision reason is required.", nameof(reason));

        var evidence = await researchStore.GetEvidenceForClaimAsync(claim.Id, cancellationToken);
        if (decision == ResearchDecision.AcceptForPromotionReview && !evidence.Any(e => e.Direction == EvidenceDirection.Supports && e.Weight > 0))
            throw new InvalidOperationException("A claim cannot be accepted for promotion review without reviewed supporting documentary evidence.");

        var record = new ApprovalRecord(Guid.NewGuid(), "ResearchClaim", claim.Id, decision, actor.Trim(), reason.Trim(), DateTimeOffset.UtcNow);
        await approvalStore.SaveApprovalAsync(record, cancellationToken);
        return record;
    }
}
