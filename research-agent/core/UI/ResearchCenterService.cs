using ResearchAgent.Core.Domain;
using ResearchAgent.Core.Evidence;
using ResearchAgent.Core.Persistence;
using ResearchAgent.Core.Research;

namespace ResearchAgent.Core.UI;

public interface IResearchCenterQueryStore : IResearchStore
{
    Task<IReadOnlyList<ResearchClaim>> GetClaimsForPersonAsync(Guid personId, CancellationToken cancellationToken = default);
    Task<int> GetSourceCountForPersonAsync(Guid personId, CancellationToken cancellationToken = default);
    Task<int> GetProofPacketCountForPersonAsync(Guid personId, CancellationToken cancellationToken = default);
}

public sealed class ResearchCenterService(IResearchCenterQueryStore store, Func<Guid, string> displayNameResolver) : IResearchCenterService
{
    public async Task<ResearchCenterSummary> GetSummaryAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        var claims = await store.GetClaimsForPersonAsync(personId, cancellationToken);
        var tasks = await store.GetOpenTasksAsync(personId, cancellationToken);
        var conflicts = ConflictDetector.Detect(claims);
        var findings = new List<ResearchFindingView>();
        foreach (var claim in claims)
        {
            var evidence = await store.GetEvidenceForClaimAsync(claim.Id, cancellationToken);
            findings.Add(new(claim.Id, claim.ClaimType, claim.ProposedValue, claim.Status, claim.Confidence,
                evidence.Count(e => e.Direction == EvidenceDirection.Supports),
                evidence.Count(e => e.Direction == EvidenceDirection.Conflicts)));
        }
        var ranked = ResearchPriorityEngine.Rank(tasks).ToDictionary(x => x.Task.Id);
        var taskViews = tasks.Select(t => new ResearchTaskView(t.Id, t.Question,
            ranked.TryGetValue(t.Id, out var p) ? p.PriorityScore : 0m, t.Status))
            .OrderByDescending(t => t.PriorityScore).ToArray();

        return new(personId, displayNameResolver(personId),
            await store.GetSourceCountForPersonAsync(personId, cancellationToken),
            claims.Count(c => c.Status is ClaimStatus.Unverified or ClaimStatus.Possible or ClaimStatus.Probable),
            conflicts.Count, tasks.Count,
            await store.GetProofPacketCountForPersonAsync(personId, cancellationToken),
            findings, taskViews);
    }
}
