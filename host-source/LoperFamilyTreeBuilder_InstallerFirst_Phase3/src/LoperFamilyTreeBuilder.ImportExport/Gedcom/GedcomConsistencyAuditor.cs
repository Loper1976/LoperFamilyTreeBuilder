namespace LoperFamilyTreeBuilder.ImportExport.Gedcom;

public enum GedcomVerificationSeverity
{
    Warning,
    Error
}

public sealed record GedcomVerificationIssue(
    string Code,
    GedcomVerificationSeverity Severity,
    string Summary,
    IReadOnlyList<string> ExternalRecordIds);

public sealed record GedcomVerificationReport(
    int IndividualsChecked,
    int FamiliesChecked,
    IReadOnlyList<GedcomVerificationIssue> Issues)
{
    public int ErrorCount => Issues.Count(x => x.Severity == GedcomVerificationSeverity.Error);
    public int WarningCount => Issues.Count(x => x.Severity == GedcomVerificationSeverity.Warning);
}

public sealed class GedcomConsistencyAuditor
{
    public GedcomVerificationReport Audit(GedcomDocument document)
    {
        var issues = new List<GedcomVerificationIssue>();
        var people = document.Individuals.ToDictionary(x => x.ExternalId, StringComparer.Ordinal);

        foreach (var person in document.Individuals)
        {
            var birthYear = person.BirthDate?.Year;
            var deathYear = person.DeathDate?.Year;
            if (birthYear.HasValue && deathYear.HasValue && deathYear < birthYear)
                issues.Add(Issue("DEATH_BEFORE_BIRTH", GedcomVerificationSeverity.Error,
                    "Death precedes birth.", person.ExternalId));
            if (birthYear.HasValue && deathYear.HasValue && deathYear - birthYear > 120)
                issues.Add(Issue("IMPLAUSIBLE_LIFESPAN", GedcomVerificationSeverity.Warning,
                    "Recorded lifespan exceeds 120 years.", person.ExternalId));
        }

        foreach (var family in document.Families)
        {
            if (family.HusbandId is not null && family.HusbandId == family.WifeId)
                issues.Add(Issue("SAME_SPOUSE", GedcomVerificationSeverity.Error,
                    "The same person is recorded as both spouses.", family.ExternalId, family.HusbandId));

            foreach (var duplicateChild in family.ChildIds.GroupBy(x => x, StringComparer.Ordinal).Where(x => x.Count() > 1))
                issues.Add(Issue("DUPLICATE_CHILD_LINK", GedcomVerificationSeverity.Warning,
                    "A child is linked to the same family more than once.", family.ExternalId, duplicateChild.Key));

            foreach (var parentId in new[] { family.HusbandId, family.WifeId }.Where(x => x is not null).Cast<string>())
            {
                if (!people.TryGetValue(parentId, out var parent)) continue;
                foreach (var childId in family.ChildIds)
                {
                    if (!people.TryGetValue(childId, out var child)) continue;
                    CheckParentChild(parent, child, family.ExternalId, issues);
                }
            }

            var marriageYear = family.MarriageDate?.Year;
            if (marriageYear.HasValue)
            {
                foreach (var spouseId in new[] { family.HusbandId, family.WifeId }.Where(x => x is not null).Cast<string>())
                {
                    if (!people.TryGetValue(spouseId, out var spouse) || spouse.BirthDate?.Year is null) continue;
                    if (marriageYear < spouse.BirthDate.Year)
                        issues.Add(Issue("MARRIAGE_BEFORE_BIRTH", GedcomVerificationSeverity.Error,
                            "Marriage precedes a spouse's birth.", family.ExternalId, spouseId));
                    else if (marriageYear - spouse.BirthDate.Year < 12)
                        issues.Add(Issue("MARRIAGE_UNDER_12", GedcomVerificationSeverity.Warning,
                            "A spouse is recorded as younger than 12 at marriage.", family.ExternalId, spouseId));
                }
            }
        }

        return new GedcomVerificationReport(
            document.Individuals.Count,
            document.Families.Count,
            issues.Distinct().ToArray());
    }

    private static void CheckParentChild(
        GedcomIndividual parent,
        GedcomIndividual child,
        string familyId,
        ICollection<GedcomVerificationIssue> issues)
    {
        var parentBirth = parent.BirthDate?.Year;
        var childBirth = child.BirthDate?.Year;
        if (parentBirth.HasValue && childBirth.HasValue)
        {
            var age = childBirth.Value - parentBirth.Value;
            if (age < 0)
                issues.Add(Issue("CHILD_BEFORE_PARENT_BIRTH", GedcomVerificationSeverity.Error,
                    "A child is born before a parent.", familyId, parent.ExternalId, child.ExternalId));
            else if (age < 12)
                issues.Add(Issue("PARENT_UNDER_12", GedcomVerificationSeverity.Warning,
                    "A parent is recorded as younger than 12 at the child's birth.", familyId, parent.ExternalId, child.ExternalId));
            else if (age > 80)
                issues.Add(Issue("PARENT_OVER_80", GedcomVerificationSeverity.Warning,
                    "A parent is recorded as older than 80 at the child's birth.", familyId, parent.ExternalId, child.ExternalId));
        }

        var parentDeath = parent.DeathDate?.Year;
        if (parentDeath.HasValue && childBirth.HasValue && childBirth > parentDeath + 1)
            issues.Add(Issue("CHILD_AFTER_PARENT_DEATH", GedcomVerificationSeverity.Warning,
                "A child is born more than one year after a parent's death.", familyId, parent.ExternalId, child.ExternalId));
    }

    private static GedcomVerificationIssue Issue(
        string code,
        GedcomVerificationSeverity severity,
        string summary,
        params string[] ids) => new(code, severity, summary, ids);
}
