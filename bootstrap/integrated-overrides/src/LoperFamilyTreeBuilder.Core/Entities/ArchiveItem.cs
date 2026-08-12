namespace LoperFamilyTreeBuilder.Core.Entities;

public enum ArchiveItemType
{
    Photo = 0,
    Document = 1,
    Audio = 2,
    Video = 3,
    Other = 4
}

public sealed class ArchiveItem
{
    private ArchiveItem() { }

    public ArchiveItem(ArchiveItemType itemType, string title, string originalPath)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("A title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(originalPath))
            throw new ArgumentException("The preserved original file path is required.", nameof(originalPath));

        Id = Guid.NewGuid();
        ItemType = itemType;
        Title = title.Trim();
        OriginalPath = originalPath.Trim();
        CreatedUtc = DateTimeOffset.UtcNow;
        ModifiedUtc = CreatedUtc;
    }

    public Guid Id { get; private set; }
    public Guid? PersonId { get; private set; }
    public Guid? SourceRecordId { get; private set; }
    public ArchiveItemType ItemType { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string OriginalPath { get; private set; } = string.Empty;
    public string Sha256 { get; private set; } = string.Empty;
    public DateTimeOffset? CapturedUtc { get; private set; }
    public string OriginalPlaceText { get; private set; } = string.Empty;
    public decimal? Latitude { get; private set; }
    public decimal? Longitude { get; private set; }
    public string Caption { get; private set; } = string.Empty;
    public string Provenance { get; private set; } = string.Empty;
    public string MetadataJson { get; private set; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; private set; }
    public DateTimeOffset ModifiedUtc { get; private set; }

    public void LinkPerson(Guid? personId) { PersonId = personId; Touch(); }
    public void LinkSource(Guid? sourceRecordId) { SourceRecordId = sourceRecordId; Touch(); }

    public void UpdateMetadata(string? sha256, DateTimeOffset? capturedUtc, string? placeText, decimal? latitude, decimal? longitude, string? caption, string? provenance, string? metadataJson)
    {
        if (latitude is < -90 or > 90) throw new ArgumentOutOfRangeException(nameof(latitude));
        if (longitude is < -180 or > 180) throw new ArgumentOutOfRangeException(nameof(longitude));
        Sha256 = (sha256 ?? string.Empty).Trim();
        CapturedUtc = capturedUtc;
        OriginalPlaceText = (placeText ?? string.Empty).Trim();
        Latitude = latitude;
        Longitude = longitude;
        Caption = caption ?? string.Empty;
        Provenance = provenance ?? string.Empty;
        MetadataJson = metadataJson ?? string.Empty;
        Touch();
    }

    private void Touch() => ModifiedUtc = DateTimeOffset.UtcNow;
}

public sealed class SourceRecord
{
    private SourceRecord() { }

    public SourceRecord(string title, string citation)
    {
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("A title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(citation)) throw new ArgumentException("A citation is required.", nameof(citation));
        Id = Guid.NewGuid();
        Title = title.Trim();
        Citation = citation.Trim();
        CreatedUtc = DateTimeOffset.UtcNow;
        ModifiedUtc = CreatedUtc;
    }

    public Guid Id { get; private set; }
    public Guid? PersonId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Citation { get; private set; } = string.Empty;
    public string Repository { get; private set; } = string.Empty;
    public string CallNumberOrUrl { get; private set; } = string.Empty;
    public string Notes { get; private set; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; private set; }
    public DateTimeOffset ModifiedUtc { get; private set; }

    public void Update(Guid? personId, string? repository, string? callNumberOrUrl, string? notes)
    {
        PersonId = personId;
        Repository = (repository ?? string.Empty).Trim();
        CallNumberOrUrl = (callNumberOrUrl ?? string.Empty).Trim();
        Notes = notes ?? string.Empty;
        ModifiedUtc = DateTimeOffset.UtcNow;
    }
}
