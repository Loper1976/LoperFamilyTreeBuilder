namespace LoperFamilyTreeBuilder.ImportExport.Gedcom;

public enum GedcomImportDisposition
{
    NewPerson,
    PossibleDuplicate,
    ExactDuplicate
}

public sealed record AcceptedPersonMatchInput(
    Guid PersonId,
    string GivenName,
    string Surname,
    DateOnly? BirthDate);

public sealed record GedcomImportCandidate(
    string ExternalId,
    GedcomPrivacyClassification Privacy,
    GedcomImportDisposition Disposition,
    Guid? MatchingPersonId);

public sealed record GedcomImportPreview(
    string GedcomVersion,
    string CharacterEncoding,
    int IndividualCount,
    int FamilyCount,
    int SourceCount,
    int MediaCount,
    int ExplicitlyDeceasedCount,
    int HistoricalByAgeCount,
    int ProtectedUncertainCount,
    int NewPersonCount,
    int PossibleDuplicateCount,
    int ExactDuplicateCount,
    IReadOnlyList<GedcomImportCandidate> Candidates,
    IReadOnlyList<string> Diagnostics)
{
    public bool CanProceedToReview => Diagnostics.Count == 0;
}

public sealed class GedcomDuplicateAnalyzer
{
    public GedcomImportPreview CreatePreview(
        GedcomDocument document,
        IReadOnlyCollection<AcceptedPersonMatchInput> acceptedPeople)
    {
        var candidates = document.Individuals.Select(person =>
        {
            var nameMatches = acceptedPeople.Where(existing =>
                Normalize(existing.GivenName) == Normalize(person.GivenName) &&
                Normalize(existing.Surname) == Normalize(person.Surname)).ToArray();
            var exact = nameMatches.FirstOrDefault(existing =>
                existing.BirthDate.HasValue &&
                person.BirthDate?.ExactDate == existing.BirthDate);
            if (exact is not null)
                return new GedcomImportCandidate(person.ExternalId, person.Privacy,
                    GedcomImportDisposition.ExactDuplicate, exact.PersonId);

            var possible = nameMatches.FirstOrDefault(existing =>
                !existing.BirthDate.HasValue ||
                person.BirthDate?.Year is null ||
                existing.BirthDate.Value.Year == person.BirthDate.Year);
            return possible is null
                ? new GedcomImportCandidate(person.ExternalId, person.Privacy,
                    GedcomImportDisposition.NewPerson, null)
                : new GedcomImportCandidate(person.ExternalId, person.Privacy,
                    GedcomImportDisposition.PossibleDuplicate, possible.PersonId);
        }).ToArray();

        return new GedcomImportPreview(
            document.Version,
            document.CharacterEncoding,
            document.Individuals.Count,
            document.Families.Count,
            document.SourceRecordCount,
            document.MediaRecordCount,
            document.Individuals.Count(x => x.Privacy == GedcomPrivacyClassification.ExplicitlyDeceased),
            document.Individuals.Count(x => x.Privacy == GedcomPrivacyClassification.HistoricalByAge),
            document.Individuals.Count(x => x.Privacy == GedcomPrivacyClassification.ProtectedUncertain),
            candidates.Count(x => x.Disposition == GedcomImportDisposition.NewPerson),
            candidates.Count(x => x.Disposition == GedcomImportDisposition.PossibleDuplicate),
            candidates.Count(x => x.Disposition == GedcomImportDisposition.ExactDuplicate),
            candidates,
            document.Diagnostics);
    }

    private static string Normalize(string value) => string.Concat(
        value.Normalize(System.Text.NormalizationForm.FormD)
            .Where(character =>
                System.Globalization.CharUnicodeInfo.GetUnicodeCategory(character) !=
                System.Globalization.UnicodeCategory.NonSpacingMark &&
                char.IsLetterOrDigit(character)))
        .ToUpperInvariant();
}
