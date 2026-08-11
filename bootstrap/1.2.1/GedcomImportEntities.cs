namespace LoperFamilyTreeBuilder.Core.Entities;

public enum GedcomImportStatus
{
    Preview = 1,
    Approved = 2,
    Applied = 3,
    Rejected = 4,
    Failed = 5,
    RolledBack = 6
}

public enum GedcomImportIssueType
{
    UnsupportedTag = 1,
    DuplicateCandidate = 2,
    Conflict = 3,
    ValidationWarning = 4,
    ValidationError = 5,
    LegacyNumberConflict = 6,
    DataQualityGap = 7
}

public sealed class GedcomImportSession
{
    private GedcomImportSession() { }

    public GedcomImportSession(string originalFileName, string storedRelativePath, string sha256,
        int individualCount, int familyCount, int sourceCount, string importPlanJson)
    {
        if (string.IsNullOrWhiteSpace(originalFileName)) throw new ArgumentException("GEDCOM filename is required.", nameof(originalFileName));
        if (string.IsNullOrWhiteSpace(storedRelativePath)) throw new ArgumentException("GEDCOM staging path is required.", nameof(storedRelativePath));
        if (string.IsNullOrWhiteSpace(sha256)) throw new ArgumentException("GEDCOM SHA-256 is required.", nameof(sha256));
        Id = Guid.NewGuid(); OriginalFileName = originalFileName; StoredRelativePath = storedRelativePath; Sha256 = sha256;
        IndividualCount = individualCount; FamilyCount = familyCount; SourceCount = sourceCount; ImportPlanJson = importPlanJson ?? string.Empty;
        Status = GedcomImportStatus.Preview; CreatedUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public string OriginalFileName { get; private set; } = string.Empty;
    public string StoredRelativePath { get; private set; } = string.Empty;
    public string Sha256 { get; private set; } = string.Empty;
    public GedcomImportStatus Status { get; private set; }
    public int IndividualCount { get; private set; }
    public int FamilyCount { get; private set; }
    public int SourceCount { get; private set; }
    public int UnsupportedTagCount { get; private set; }
    public int DuplicateCandidateCount { get; private set; }
    public int ConflictCount { get; private set; }
    public string ImportPlanJson { get; private set; } = string.Empty;
    public string Notes { get; private set; } = string.Empty;
    public string? BackupFilePath { get; private set; }
    public DateTimeOffset CreatedUtc { get; private set; }
    public DateTimeOffset? ApprovedUtc { get; private set; }
    public DateTimeOffset? AppliedUtc { get; private set; }

    public void SetIssueCounts(int unsupported, int duplicates, int conflicts)
    { UnsupportedTagCount = Math.Max(0, unsupported); DuplicateCandidateCount = Math.Max(0, duplicates); ConflictCount = Math.Max(0, conflicts); }

    public void Approve()
    {
        if (Status != GedcomImportStatus.Preview) throw new InvalidOperationException("Only a preview import may be approved.");
        Status = GedcomImportStatus.Approved; ApprovedUtc = DateTimeOffset.UtcNow;
    }

    public void MarkApplied(string backupFilePath)
    {
        if (Status != GedcomImportStatus.Approved) throw new InvalidOperationException("Only an approved import may be applied.");
        BackupFilePath = backupFilePath; Status = GedcomImportStatus.Applied; AppliedUtc = DateTimeOffset.UtcNow;
    }

    public void MarkRolledBack(string notes)
    {
        if (Status != GedcomImportStatus.Applied) throw new InvalidOperationException("Only an applied import may be rolled back.");
        Notes = notes ?? string.Empty; Status = GedcomImportStatus.RolledBack;
    }

    public void Reject(string notes)
    {
        if (Status == GedcomImportStatus.Applied) throw new InvalidOperationException("An applied import cannot be rejected.");
        Notes = notes ?? string.Empty; Status = GedcomImportStatus.Rejected;
    }

    public void MarkFailed(string notes) { Notes = notes ?? string.Empty; Status = GedcomImportStatus.Failed; }
}

public sealed class GedcomImportIssue
{
    private GedcomImportIssue() { }
    public GedcomImportIssue(Guid importSessionId, GedcomImportIssueType issueType, string recordPointer, string message, string details = "")
    { Id = Guid.NewGuid(); ImportSessionId = importSessionId; IssueType = issueType; RecordPointer = recordPointer ?? string.Empty; Message = message ?? string.Empty; Details = details ?? string.Empty; CreatedUtc = DateTimeOffset.UtcNow; }
    public Guid Id { get; private set; }
    public Guid ImportSessionId { get; private set; }
    public GedcomImportIssueType IssueType { get; private set; }
    public string RecordPointer { get; private set; } = string.Empty;
    public string Message { get; private set; } = string.Empty;
    public string Details { get; private set; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; private set; }
    public GedcomImportSession ImportSession { get; private set; } = null!;
}

public sealed class GedcomImportedNote
{
    private GedcomImportedNote() { }
    public GedcomImportedNote(Guid importSessionId, Guid? personId, string recordPointer, string text)
    {
        Id = Guid.NewGuid(); ImportSessionId = importSessionId; PersonId = personId; RecordPointer = recordPointer ?? string.Empty;
        Text = text ?? string.Empty; CreatedUtc = DateTimeOffset.UtcNow;
    }
    public Guid Id { get; private set; }
    public Guid ImportSessionId { get; private set; }
    public Guid? PersonId { get; private set; }
    public string RecordPointer { get; private set; } = string.Empty;
    public string Text { get; private set; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; private set; }
    public GedcomImportSession ImportSession { get; private set; } = null!;
    public Person? Person { get; private set; }
}
