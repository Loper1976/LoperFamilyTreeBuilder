using LoperFamilyTreeBuilder.Core.Entities;

namespace LoperFamilyTreeBuilder.Core.Models;

public sealed class GedcomPreviewRequest
{
    public string OriginalFileName { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
}

public sealed record GedcomImportSessionListItem(Guid Id, string OriginalFileName, GedcomImportStatus Status,
    int IndividualCount, int FamilyCount, int SourceCount, int UnsupportedTagCount, int DuplicateCandidateCount,
    int ConflictCount, DateTimeOffset CreatedUtc, DateTimeOffset? ApprovedUtc, DateTimeOffset? AppliedUtc)
{
    public string SessionCode => $"GEDCOM-{CreatedUtc:yyyyMMdd}-{Id.ToString("N")[..6].ToUpperInvariant()}";
}

public sealed record GedcomImportIssueListItem(GedcomImportIssueType IssueType, string RecordPointer, string Message, string Details);

public sealed record GedcomDataQualitySummary(
    int MissingBirthDateCount,
    int MissingDeathDateCount,
    int MissingParentLinkCount,
    int MissingSourceCount,
    int LegacyNumberCount,
    int LegacyNumberConflictCount,
    int NoteCount,
    int PlaceVariantGroupCount);

public sealed record GedcomImportReviewModel(
    GedcomImportSessionListItem Session,
    IReadOnlyList<GedcomImportIssueListItem> Issues,
    IReadOnlyList<GedcomPlannedPerson> PlannedPeople,
    IReadOnlyList<GedcomPlannedFamily> PlannedFamilies,
    GedcomDataQualitySummary Quality);

public sealed record GedcomPlannedPerson(
    string Pointer,
    string GivenName,
    string Surname,
    string DisplayName,
    DateOnly? BirthDate,
    DateOnly? DeathDate,
    string BirthPlace,
    string DeathPlace,
    string LegacyNumber,
    string ExternalId,
    int NoteCount,
    int SourceReferenceCount,
    bool IsDuplicate,
    Guid? MatchedPersonId,
    string DuplicateReason,
    bool HasLegacyNumberConflict,
    bool WillCreate);

public sealed record GedcomPlannedFamily(
    string Pointer,
    string? Spouse1Pointer,
    string? Spouse2Pointer,
    IReadOnlyList<string> ChildPointers,
    DateOnly? MarriageDate,
    string MarriagePlace,
    bool CanApply,
    string Note);

public sealed record GedcomValidationReport(bool IsValid, string FileName, int LineCount, int IndividualCount,
    int FamilyCount, int SourceCount, IReadOnlyList<string> Errors, IReadOnlyList<string> Warnings, IReadOnlyList<string> UnsupportedTags);

public sealed record GedcomDryRunReport(
    string FileName,
    bool IsValid,
    int LineCount,
    int IndividualCount,
    int FamilyCount,
    int SourceCount,
    int DuplicateCandidateCount,
    int ConflictCount,
    int LegacyNumberConflictCount,
    int UnsupportedTagCount,
    GedcomDataQualitySummary Quality,
    IReadOnlyList<GedcomImportIssueListItem> Issues,
    IReadOnlyList<GedcomPlannedPerson> SamplePeople)
{
    public bool CanStage => IsValid;
}

public sealed record GedcomApplyResult(int PeopleCreated, int ParentChildRelationshipsCreated,
    int CoupleRelationshipsCreated, int SourcesCreated, int CitationsCreated, int NotesPreserved,
    int SkippedDuplicates, string BackupFilePath);

public sealed record GedcomRollbackResult(int PeopleRemoved, int ParentChildRelationshipsRemoved,
    int CoupleRelationshipsRemoved, int CitationsRemoved, int NotesRemoved, int SourcesRemoved);
