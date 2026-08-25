using System.Text.Json;
using LoperFamilyTreeBuilder.Core.Entities;
using Microsoft.EntityFrameworkCore;
using ResearchAgent.Core.Integration;
using ResearchAgent.Core.Domain;
using ResearchAgent.Core.Persistence;
using ResearchAgent.Core.Review;

namespace LoperFamilyTreeBuilder.Data.Services;

public sealed class HostPersonReader(IDbContextFactory<FamilyTreeDbContext> contexts) : IHostPersonReader
{
    public async Task<HostPerson?> GetPersonAsync(Guid stablePersonId, CancellationToken cancellationToken = default)
    {
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        return await db.People.AsNoTracking().Where(x => x.Id == stablePersonId)
            .Select(x => new HostPerson(x.Id, x.DisplayName,
                x.Identifiers.Where(i => i.IdentifierType == PersonIdentifierType.LegacyNumber)
                    .Select(i => i.Value).FirstOrDefault(),
                x.BirthDate, null, x.DeathDate, null))
            .SingleOrDefaultAsync(cancellationToken);
    }
}

public sealed class HostApprovalStore(IDbContextFactory<FamilyTreeDbContext> contexts) : IApprovalStore
{
    public async Task SaveApprovalAsync(ApprovalRecord record, CancellationToken cancellationToken = default)
    {
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        db.ResearchApprovals.Add(new ResearchApproval(record.Id, record.EntityType, record.EntityId,
            record.Decision.ToString(), record.Actor, record.Reason, record.CreatedUtc));
        db.AuditEvents.Add(new AuditEvent("ResearchDecision", record.EntityType, record.EntityId.ToString(),
            record.Actor, $"Research decision recorded: {record.Decision}.",
            newValueJson: JsonSerializer.Serialize(record)));
        await db.SaveChangesAsync(cancellationToken);
    }
}

public sealed class AcceptedTreePromotionService(
    IDbContextFactory<FamilyTreeDbContext> contexts,
    IResearchStore researchStore) : IAcceptedTreePromotionService
{
    public async Task<Guid> PromoteClaimAsync(Guid researchClaimId, ApprovalRecord approval,
        CancellationToken cancellationToken = default)
    {
        if (approval.EntityType != "ResearchClaim" || approval.EntityId != researchClaimId ||
            approval.Decision != ResearchDecision.AcceptForPromotionReview)
            throw new InvalidOperationException("A matching AcceptForPromotionReview approval is required.");

        var claim = await researchStore.GetClaimAsync(researchClaimId, cancellationToken)
            ?? throw new InvalidOperationException("The research claim does not exist.");
        if (claim.Status is ClaimStatus.Rejected or ClaimStatus.Conflicting or ClaimStatus.Unverified)
            throw new InvalidOperationException("Rejected, conflicting, or unverified claims cannot enter the accepted tree.");

        var evidence = await researchStore.GetEvidenceForClaimAsync(claim.Id, cancellationToken);
        if (!evidence.Any(x => x.Direction == EvidenceDirection.Supports && x.Weight > 0))
            throw new InvalidOperationException("Reviewed supporting documentary evidence is required.");

        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        var persistedApproval = await db.ResearchApprovals.AsNoTracking()
            .AnyAsync(x => x.Id == approval.Id && x.EntityId == claim.Id &&
                x.Decision == nameof(ResearchDecision.AcceptForPromotionReview), cancellationToken);
        if (!persistedApproval) throw new InvalidOperationException("The approval has not been persisted by the host.");
        if (!await db.People.AnyAsync(x => x.Id == claim.SubjectPersonId, cancellationToken))
            throw new InvalidOperationException("The accepted-tree person does not exist.");

        var fact = new AcceptedFact(claim.SubjectPersonId, claim.Id, approval.Id,
            claim.ClaimType, claim.ProposedValue, approval.Actor);
        db.AcceptedFacts.Add(fact);
        db.AuditEvents.Add(new AuditEvent("PromoteResearchClaim", nameof(AcceptedFact), fact.Id.ToString(),
            approval.Actor, $"Promoted reviewed {claim.ClaimType} claim into the accepted tree.",
            newValueJson: JsonSerializer.Serialize(new { fact.Id, fact.PersonId, fact.ResearchClaimId,
                fact.ApprovalId, fact.FactType, fact.Value })));
        await db.SaveChangesAsync(cancellationToken);
        return fact.Id;
    }
}
