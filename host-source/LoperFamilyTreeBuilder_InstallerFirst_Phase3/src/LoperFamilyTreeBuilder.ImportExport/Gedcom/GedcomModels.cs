namespace LoperFamilyTreeBuilder.ImportExport.Gedcom;

public enum GedcomPrivacyClassification
{
    ExplicitlyDeceased,
    HistoricalByAge,
    ProtectedUncertain
}

public sealed record GedcomDate(string OriginalText, DateOnly? ExactDate, int? Year);

public sealed record GedcomIndividual(
    string ExternalId,
    string GivenName,
    string Surname,
    string Sex,
    GedcomDate? BirthDate,
    string? BirthPlace,
    GedcomDate? DeathDate,
    string? DeathPlace,
    bool HasDeathRecord,
    GedcomPrivacyClassification Privacy,
    IReadOnlyList<string> FamilyAsChildIds,
    IReadOnlyList<string> FamilyAsSpouseIds,
    GedcomDate? BurialDate = null,
    string? BurialPlace = null);

public sealed record GedcomFamily(
    string ExternalId,
    string? HusbandId,
    string? WifeId,
    IReadOnlyList<string> ChildIds,
    GedcomDate? MarriageDate,
    string? MarriagePlace);

public sealed record GedcomDocument(
    string Version,
    string CharacterEncoding,
    IReadOnlyList<GedcomIndividual> Individuals,
    IReadOnlyList<GedcomFamily> Families,
    int SourceRecordCount,
    int MediaRecordCount,
    IReadOnlyList<string> Diagnostics)
{
    public bool IsStructurallyValid => Diagnostics.Count == 0;
    public int ResearchEligibleCount => Individuals.Count(x =>
        x.Privacy != GedcomPrivacyClassification.ProtectedUncertain);
}
