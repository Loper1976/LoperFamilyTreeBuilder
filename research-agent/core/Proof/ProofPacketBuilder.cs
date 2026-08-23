using System.Text.Json;
using ResearchAgent.Core.Domain;
using ResearchAgent.Core.Evidence;

namespace ResearchAgent.Core.Proof;

public sealed record ProofPacketInput(
    Guid SubjectPersonId, string Question, ResearchClaim Claim,
    IReadOnlyList<ResearchSource> Sources, IReadOnlyList<Citation> Citations,
    IReadOnlyList<EvidenceLink> Evidence, IReadOnlyList<AiAnalysis> Analyses,
    IReadOnlyList<string> UnresolvedQuestions);

public sealed record ProofPacket(
    Guid Id, Guid SubjectPersonId, string Question, DateTimeOffset GeneratedUtc,
    ClaimStatus FinalStatus, decimal DeterministicScore, string PacketJson);

public static class ProofPacketBuilder
{
    public static ProofPacket Build(ProofPacketInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        var assessment = EvidenceScorer.Assess(input.Evidence);

        var payload = new
        {
            schemaVersion = 1,
            subjectPersonId = input.SubjectPersonId,
            question = input.Question,
            claim = input.Claim,
            documentaryAssessment = assessment,
            sources = input.Sources,
            citations = input.Citations,
            evidence = input.Evidence,
            aiAnalyses = input.Analyses.Select(a => new
            {
                a.Provider, a.Model, a.Role, a.ResponseText, a.Confidence, a.CreatedUtc,
                evidentiaryWeight = 0
            }),
            unresolvedQuestions = input.UnresolvedQuestions,
            safeguards = new
            {
                aiIsEvidence = false,
                autoMergeAllowed = false,
                acceptedTreePromotionPerformed = false
            }
        };

        return new ProofPacket(Guid.NewGuid(), input.SubjectPersonId, input.Question,
            DateTimeOffset.UtcNow, assessment.Status, assessment.Score,
            JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
    }
}
