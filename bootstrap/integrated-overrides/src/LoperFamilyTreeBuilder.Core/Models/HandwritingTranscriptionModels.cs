using LoperFamilyTreeBuilder.Core.Entities;

namespace LoperFamilyTreeBuilder.Core.Models;

public sealed record HandwritingTranscriptionDashboard(
    int VisibleRecords,
    int Queued,
    int DraftReady,
    int NeedsReview,
    int Approved,
    int Failed);

public sealed record HandwritingTranscriptionQueueItem(
    Guid Id,
    Guid? PersonId,
    string PersonDisplayName,
    string? LegacyNumber,
    string DocumentTitle,
    string ArchiveRelativePath,
    HandwritingTranscriptionStatus Status,
    HandwritingTranscriptionVisibility Visibility,
    string ProviderName,
    string ModelName,
    decimal? Confidence,
    DateTimeOffset ModifiedUtc);

public sealed record HandwritingTranscriptionDetail(
    Guid Id,
    Guid? PersonId,
    string PersonDisplayName,
    string? LegacyNumber,
    string DocumentTitle,
    string ArchiveRelativePath,
    string OriginalImageHashSha256,
    string SourceCitation,
    HandwritingTranscriptionStatus Status,
    HandwritingTranscriptionVisibility Visibility,
    string ProviderName,
    string ModelName,
    decimal? Confidence,
    string MachineDraft,
    string ReviewedTranscript,
    string ApprovedTranscript,
    string FailureMessage,
    DateTimeOffset CreatedUtc,
    DateTimeOffset ModifiedUtc,
    DateTimeOffset? ApprovedUtc);
