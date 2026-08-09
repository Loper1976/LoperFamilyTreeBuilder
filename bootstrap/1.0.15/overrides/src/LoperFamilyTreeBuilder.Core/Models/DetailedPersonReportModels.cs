namespace LoperFamilyTreeBuilder.Core.Models;

public enum DetailedReportMode
{
    Archival,
    Family,
    PublicSafe
}

public sealed record DetailedPersonReport(
    Guid PersonId,
    string DisplayName,
    string? LegacyNumber,
    string? Lifespan,
    bool IsDeceased,
    string? PrimaryPhotoReference,
    string? BranchName,
    DateTimeOffset GeneratedUtc,
    IReadOnlyList<PersonReportFact> VitalFacts,
    IReadOnlyList<PersonReportRelationship> FamilyRelationships,
    IReadOnlyList<PersonReportTimelineItem> Timeline,
    IReadOnlyList<PersonReportSection> Sections,
    DetailedReportMode Mode);

public sealed record PersonReportFact(string Label, string Value);

public sealed record PersonReportRelationship(
    string Relationship,
    string PersonName,
    Guid? RelatedPersonId,
    string? Detail = null);

public sealed record PersonReportTimelineItem(
    string? Date,
    string Event,
    string? Place,
    string? Detail,
    string? Source = null);

public sealed record PersonReportSection(
    string Key,
    string Title,
    IReadOnlyList<PersonReportRecord> Records,
    bool IsMedical = false,
    bool IsRestricted = false);

public sealed record PersonReportRecord(
    string Heading,
    IReadOnlyList<PersonReportFact> Facts,
    string? Narrative = null);
