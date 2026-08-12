using System.Globalization;

namespace LoperFamilyTreeBuilder.Core.Entities;

public sealed class HandwritingTranscription
{
    private HandwritingTranscription()
    {
    }

    public HandwritingTranscription(
        string documentTitle,
        string archiveRelativePath,
        string? sourceCitation = null,
        Guid? personId = null)
    {
        if (string.IsNullOrWhiteSpace(documentTitle))
            throw new ArgumentException("A document title is required.", nameof(documentTitle));

        if (string.IsNullOrWhiteSpace(archiveRelativePath))
            throw new ArgumentException("The preserved archive path is required.", nameof(archiveRelativePath));

        if (personId == Guid.Empty)
            throw new ArgumentException("A person identifier cannot be empty.", nameof(personId));

        Id = Guid.NewGuid();
        PersonId = personId;
        DocumentTitle = documentTitle.Trim();
        ArchiveRelativePath = archiveRelativePath.Trim();
        SourceCitation = (sourceCitation ?? string.Empty).Trim();
        Status = HandwritingTranscriptionStatus.Queued;
        Visibility = HandwritingTranscriptionVisibility.OwnerOnly;
        CreatedUtc = DateTimeOffset.UtcNow;
        ModifiedUtc = CreatedUtc;
    }

    public Guid Id { get; private set; }

    public Guid? PersonId { get; private set; }

    public Person? Person { get; private set; }

    public string DocumentTitle { get; private set; } = string.Empty;

    public string ArchiveRelativePath { get; private set; } = string.Empty;

    public string OriginalImageHashSha256 { get; private set; } = string.Empty;

    public string SourceCitation { get; private set; } = string.Empty;

    public HandwritingTranscriptionStatus Status { get; private set; }

    public HandwritingTranscriptionVisibility Visibility { get; private set; }

    public string ProviderName { get; private set; } = string.Empty;

    public string ModelName { get; private set; } = string.Empty;

    public decimal? Confidence { get; private set; }

    public string MachineDraft { get; private set; } = string.Empty;

    public string ReviewedTranscript { get; private set; } = string.Empty;

    public string ApprovedTranscript { get; private set; } = string.Empty;

    public string FailureMessage { get; private set; } = string.Empty;

    public DateTimeOffset CreatedUtc { get; private set; }

    public DateTimeOffset ModifiedUtc { get; private set; }

    public DateTimeOffset? ApprovedUtc { get; private set; }

    public void SetOriginalIntegrityHash(string? sha256)
    {
        var normalized = (sha256 ?? string.Empty).Trim().ToUpperInvariant();
        if (normalized.Length > 0 &&
            (normalized.Length != 64 || normalized.Any(character => !Uri.IsHexDigit(character))))
        {
            throw new ArgumentException("SHA-256 must contain exactly 64 hexadecimal characters.", nameof(sha256));
        }

        OriginalImageHashSha256 = normalized;
        Touch();
    }

    public void SetVisibility(HandwritingTranscriptionVisibility visibility)
    {
        Visibility = visibility;
        Touch();
    }

    public void RecordMachineDraft(
        string transcript,
        string? providerName,
        string? modelName,
        decimal? confidence)
    {
        if (string.IsNullOrWhiteSpace(transcript))
            throw new ArgumentException("The machine transcription draft cannot be empty.", nameof(transcript));

        if (confidence is < 0m or > 1m)
            throw new ArgumentOutOfRangeException(nameof(confidence), "Confidence must be between 0 and 1.");

        MachineDraft = transcript;
        ProviderName = (providerName ?? string.Empty).Trim();
        ModelName = (modelName ?? string.Empty).Trim();
        Confidence = confidence;
        ReviewedTranscript = string.Empty;
        ApprovedTranscript = string.Empty;
        ApprovedUtc = null;
        FailureMessage = string.Empty;
        Status = HandwritingTranscriptionStatus.DraftReady;
        Touch();
    }

    public void SaveReviewedTranscript(string transcript)
    {
        if (string.IsNullOrWhiteSpace(MachineDraft))
            throw new InvalidOperationException("A machine draft must exist before review text can be saved.");

        if (string.IsNullOrWhiteSpace(transcript))
            throw new ArgumentException("Reviewed transcription text cannot be empty.", nameof(transcript));

        ReviewedTranscript = transcript;
        ApprovedTranscript = string.Empty;
        ApprovedUtc = null;
        Status = HandwritingTranscriptionStatus.NeedsReview;
        Touch();
    }

    public void Approve()
    {
        var finalText = string.IsNullOrWhiteSpace(ReviewedTranscript)
            ? MachineDraft
            : ReviewedTranscript;

        if (string.IsNullOrWhiteSpace(finalText))
            throw new InvalidOperationException("A transcription draft is required before approval.");

        ApprovedTranscript = finalText;
        Status = HandwritingTranscriptionStatus.Approved;
        ApprovedUtc = DateTimeOffset.UtcNow;
        FailureMessage = string.Empty;
        Touch();
    }

    public void MarkFailed(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("A failure reason is required.", nameof(message));

        FailureMessage = message.Trim();
        Status = HandwritingTranscriptionStatus.Failed;
        Touch();
    }

    public void Requeue()
    {
        FailureMessage = string.Empty;
        Status = HandwritingTranscriptionStatus.Queued;
        Touch();
    }

    private void Touch()
    {
        ModifiedUtc = DateTimeOffset.UtcNow;
    }
}
