using ResearchAgent.Core.Domain;
using ResearchAgent.Core.Extraction;
using ResearchAgent.Core.Ingestion;
using ResearchAgent.Core.Persistence;

namespace ResearchAgent.Core.Workflow;

public sealed record DocumentIntakeResult(
    ResearchSource Source, DocumentExtractionResult Extraction,
    IReadOnlyList<ResearchClaim> ProposedClaims);

public sealed class DocumentIntakeWorkflow(
    SourceIngestionService ingestion, IResearchStore store, IDocumentExtractor extractor)
{
    public async Task<DocumentIntakeResult> RunAsync(
        SourceIngestionRequest request, Guid? targetPersonId = null,
        CancellationToken cancellationToken = default)
    {
        var source = await ingestion.IngestAsync(request, cancellationToken);
        await store.SaveSourceAsync(source, cancellationToken);

        var archivedPath = Path.Combine(Path.GetFullPath(request.ArchiveRoot),
            source.ArchivedRelativePath.Replace('/', Path.DirectorySeparatorChar));
        var extraction = await extractor.ExtractAsync(source, archivedPath, cancellationToken);

        IReadOnlyList<ResearchClaim> claims = [];
        if (targetPersonId is Guid personId)
        {
            claims = ProposalFactory.ToResearchClaims(personId, extraction);
            foreach (var claim in claims) await store.SaveClaimAsync(claim, cancellationToken);
        }

        // No accepted-tree writes occur here. The workflow stops at research proposals.
        return new DocumentIntakeResult(source, extraction, claims);
    }
}
