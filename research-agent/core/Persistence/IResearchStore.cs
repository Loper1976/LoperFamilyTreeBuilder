using ResearchAgent.Core.Domain;
using ResearchAgent.Core.Proof;
using ResearchAgent.Core.Research;

namespace ResearchAgent.Core.Persistence;

public interface IResearchStore
{
    Task SaveSourceAsync(ResearchSource source, CancellationToken cancellationToken = default);
    Task SaveCitationAsync(Citation citation, CancellationToken cancellationToken = default);
    Task SaveClaimAsync(ResearchClaim claim, CancellationToken cancellationToken = default);
    Task SaveEvidenceAsync(EvidenceLink evidence, CancellationToken cancellationToken = default);
    Task SaveAiAnalysisAsync(AiAnalysis analysis, CancellationToken cancellationToken = default);
    Task SaveResearchTaskAsync(ResearchTask task, CancellationToken cancellationToken = default);
    Task SaveProofPacketAsync(ProofPacket packet, CancellationToken cancellationToken = default);

    Task<ResearchSource?> GetSourceAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ResearchClaim?> GetClaimAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EvidenceLink>> GetEvidenceForClaimAsync(Guid claimId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ResearchTask>> GetOpenTasksAsync(Guid? personId = null, CancellationToken cancellationToken = default);
}

// Accepted-tree writes intentionally are not part of this interface.
// Promotion into the accepted tree must pass through the host application's
// backup/audit/approval safeguards after integration with its existing schema.
