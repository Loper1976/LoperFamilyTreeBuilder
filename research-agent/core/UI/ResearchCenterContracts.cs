using ResearchAgent.Core.Domain;
using ResearchAgent.Core.Research;

namespace ResearchAgent.Core.UI;

public sealed record ResearchCenterSummary(
    Guid PersonId, string DisplayName, int SourceCount, int OpenClaimCount,
    int ConflictCount, int OpenTaskCount, int ProofPacketCount,
    IReadOnlyList<ResearchFindingView> Findings,
    IReadOnlyList<ResearchTaskView> Tasks);

public sealed record ResearchFindingView(
    Guid ClaimId, string ClaimType, string ProposedValue,
    ClaimStatus Status, decimal Confidence, int SupportingEvidenceCount,
    int ConflictingEvidenceCount);

public sealed record ResearchTaskView(
    Guid TaskId, string Question, decimal PriorityScore, ResearchTaskStatus Status);

public interface IResearchCenterService
{
    Task<ResearchCenterSummary> GetSummaryAsync(Guid personId, CancellationToken cancellationToken = default);
}

// Host UI target:
// Research Center -> person -> sources / findings / conflicts / tasks / proof packets.
// UI may accept or reject proposals only through the host application's audited
// approval service. This research-core contract exposes no direct accepted-tree mutation.
