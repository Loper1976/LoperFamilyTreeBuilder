using ResearchAgent.Core.Domain;
using ResearchAgent.Core.Review;

namespace ResearchAgent.Core.Integration;

public sealed record HostPerson(Guid StablePersonId, string DisplayName, string? LegacyNumber,
    DateOnly? BirthDate, string? BirthPlace, DateOnly? DeathDate, string? DeathPlace);

public interface IHostPersonReader
{
    Task<HostPerson?> GetPersonAsync(Guid stablePersonId, CancellationToken cancellationToken = default);
}

public interface IAcceptedTreePromotionService
{
    // Implemented only by the host application. ResearchAgent.Core deliberately
    // cannot write accepted facts directly.
    Task<Guid> PromoteClaimAsync(Guid researchClaimId, ApprovalRecord approval,
        CancellationToken cancellationToken = default);
}

public interface IHostBackupGate
{
    Task<string> CreatePreChangeBackupAsync(string reason, CancellationToken cancellationToken = default);
    Task VerifyBackupAsync(string backupReference, CancellationToken cancellationToken = default);
}

public sealed class GuardedPromotionCoordinator(IHostBackupGate backupGate, IAcceptedTreePromotionService promotion)
{
    public async Task<Guid> PromoteAsync(Guid claimId, ApprovalRecord approval, CancellationToken cancellationToken = default)
    {
        if (approval.EntityId != claimId || approval.Decision != ResearchDecision.AcceptForPromotionReview)
            throw new InvalidOperationException("A matching AcceptForPromotionReview approval is required.");
        var backup = await backupGate.CreatePreChangeBackupAsync($"Promote research claim {claimId}", cancellationToken);
        await backupGate.VerifyBackupAsync(backup, cancellationToken);
        return await promotion.PromoteClaimAsync(claimId, approval, cancellationToken);
    }
}
