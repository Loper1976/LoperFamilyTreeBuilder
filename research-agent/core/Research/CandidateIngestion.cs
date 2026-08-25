using ResearchAgent.Core.Domain;
using ResearchAgent.Core.Extraction;
using ResearchAgent.Core.Ingestion;
using ResearchAgent.Core.Persistence;
using ResearchAgent.Core.Workflow;

namespace ResearchAgent.Core.Research;

public sealed record CandidateAcquisition(SourceSearchResult Candidate, string LocalFilePath,
    string Title, PrivacyClass PrivacyClass, string? SourceType = null);

public sealed class CandidateIngestionService(ProvenanceIntakeWorkflow workflow)
{
    public Task<ProvenanceIntakeResult> IngestAsync(CandidateAcquisition acquisition,
        string archiveRoot, Guid personId, CancellationToken cancellationToken = default)
    {
        if (!acquisition.Candidate.CanArchiveOriginal)
            throw new InvalidOperationException("This source provider does not permit/offer archiving the original record. Preserve citation metadata only instead.");

        var request = new SourceIngestionRequest(acquisition.LocalFilePath, archiveRoot,
            acquisition.Title, acquisition.SourceType ?? acquisition.Candidate.RecordType,
            acquisition.Candidate.Repository ?? acquisition.Candidate.Provider,
            acquisition.Candidate.Url, acquisition.PrivacyClass, DateTimeOffset.UtcNow,
            acquisition.Candidate.Summary);
        return workflow.RunAsync(request, personId, cancellationToken);
    }
}

public sealed record CitationOnlyCandidate(Guid SourceId, string Provider, string Title, Uri? Url,
    string? Repository, string? RecordType, string? Summary, DateTimeOffset CapturedUtc);

public static class CitationOnlyCapture
{
    public static CitationOnlyCandidate Capture(SourceSearchResult candidate) =>
        new(Guid.NewGuid(), candidate.Provider, candidate.Title, candidate.Url,
            candidate.Repository, candidate.RecordType, candidate.Summary, DateTimeOffset.UtcNow);
}
