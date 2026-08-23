using ResearchAgent.Core.Domain;

namespace ResearchAgent.Core.AI;

public sealed record ReviewRequest(Guid PersonId, Guid? ClaimId, string Question, string EvidencePacket, PrivacyClass PrivacyClass);
public sealed record AdversarialReviewResult(string ResearcherResponse, string SkepticResponse, string? SynthesizerResponse);

public sealed class AdversarialReviewService(
    IStructuredModelClient researcher, IStructuredModelClient skeptic,
    IStructuredModelClient? synthesizer, RoutingPolicy policy)
{
    public async Task<AdversarialReviewResult> ReviewAsync(ReviewRequest request, CancellationToken cancellationToken = default)
    {
        EnsureAllowed(researcher, request.PrivacyClass);
        EnsureAllowed(skeptic, request.PrivacyClass);
        if (synthesizer is not null) EnsureAllowed(synthesizer, request.PrivacyClass);

        var researchPrompt = "Analyze only the supplied genealogy evidence. Propose the best-supported conclusion, list contradictions and missing records. AI opinion is not evidence. Return JSON.";
        var skepticPrompt = "Try to disprove the proposed genealogy conclusion using only the supplied evidence. Look for duplicate identities, chronology, place, household, relative, occupation and negative-evidence conflicts. Return JSON.";
        var researcherResponse = await researcher.CompleteJsonAsync(researchPrompt, request.EvidencePacket, cancellationToken);
        var skepticInput = request.EvidencePacket + "\nRESEARCHER ANALYSIS (not evidence):\n" + researcherResponse;
        var skepticResponse = await skeptic.CompleteJsonAsync(skepticPrompt, skepticInput, cancellationToken);

        string? synthesis = null;
        if (synthesizer is not null)
        {
            var synthesisPrompt = "Compare researcher and skeptic analyses. Do not vote and do not treat either analysis as evidence. Identify agreements, unresolved conflicts, and the single best next record to seek. Return JSON.";
            synthesis = await synthesizer.CompleteJsonAsync(synthesisPrompt,
                skepticInput + "\nSKEPTIC ANALYSIS (not evidence):\n" + skepticResponse, cancellationToken);
        }
        return new(researcherResponse, skepticResponse, synthesis);
    }

    private void EnsureAllowed(IStructuredModelClient client, PrivacyClass privacy)
    {
        if (!PrivacyRouter.IsTierAllowed(privacy, client.Tier, policy))
            throw new InvalidOperationException($"{client.Provider}/{client.Model} is not permitted for {privacy} data.");
    }
}
