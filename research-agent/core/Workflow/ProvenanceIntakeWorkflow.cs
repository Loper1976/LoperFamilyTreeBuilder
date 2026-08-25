using ResearchAgent.Core.Domain;
using ResearchAgent.Core.Extraction;
using ResearchAgent.Core.Ingestion;
using ResearchAgent.Core.Persistence;

namespace ResearchAgent.Core.Workflow;

public sealed record ProvenanceIntakeResult(
    ResearchSource Source, DocumentExtractionResult Extraction,
    IReadOnlyList<ProposedClaimWithCitation> Proposals);

public sealed class ProvenanceIntakeWorkflow(
    SourceIngestionService ingestion, IResearchStore store, IDocumentExtractor extractor)
{
    public async Task<ProvenanceIntakeResult> RunAsync(SourceIngestionRequest request, Guid personId,
        CancellationToken cancellationToken = default)
    {
        var source = await ingestion.IngestAsync(request, cancellationToken);
        await store.SaveSourceAsync(source, cancellationToken);

        var path = Path.Combine(Path.GetFullPath(request.ArchiveRoot),
            source.ArchivedRelativePath.Replace('/', Path.DirectorySeparatorChar));
        var extraction = await extractor.ExtractAsync(source, path, cancellationToken);
        var proposals = CitationProposalService.Build(personId, source, extraction);

        foreach (var item in proposals)
        {
            await store.SaveClaimAsync(item.Claim, cancellationToken);
            await store.SaveCitationAsync(item.Citation, cancellationToken);
            await store.SaveEvidenceAsync(item.Evidence, cancellationToken);
        }

        return new(source, extraction, proposals);
    }
}
