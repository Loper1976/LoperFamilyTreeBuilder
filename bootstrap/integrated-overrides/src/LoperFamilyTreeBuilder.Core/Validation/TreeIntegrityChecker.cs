using LoperFamilyTreeBuilder.Core.Entities;

namespace LoperFamilyTreeBuilder.Core.Validation;

public sealed class TreeIntegrityChecker
{
    private readonly TreeIntegrityCheckOptions _options;

    public TreeIntegrityChecker(TreeIntegrityCheckOptions? options = null)
    {
        _options = options ?? new TreeIntegrityCheckOptions();
        _options.Validate();
    }

    public IReadOnlyList<TreeIntegrityIssue> Check(
        IEnumerable<Person> people,
        IEnumerable<ParentChildRelationship> relationships)
    {
        ArgumentNullException.ThrowIfNull(people);
        ArgumentNullException.ThrowIfNull(relationships);

        var peopleById = people
            .GroupBy(person => person.Id)
            .ToDictionary(group => group.Key, group => group.First());

        var relationshipList = relationships.ToList();
        var issues = new List<TreeIntegrityIssue>();

        CheckPersonDates(peopleById.Values, issues);
        CheckRelationshipStructure(peopleById, relationshipList, issues);
        CheckDuplicateAndConflictingRelationships(relationshipList, issues);

        return issues
            .OrderByDescending(issue => issue.Severity)
            .ThenBy(issue => issue.Code)
            .ThenBy(issue => issue.PrimaryPersonId)
            .ThenBy(issue => issue.RelatedPersonId)
            .ToArray();
    }

    private static void CheckPersonDates(
        IEnumerable<Person> people,
        ICollection<TreeIntegrityIssue> issues)
    {
        foreach (var person in people)
        {
            if (person.BirthDate.HasValue &&
                person.DeathDate.HasValue &&
                person.DeathDate.Value < person.BirthDate.Value)
            {
                issues.Add(new TreeIntegrityIssue(
                    TreeIntegrityIssueCode.DeathBeforeBirth,
                    TreeIntegrityIssueSeverity.Error,
                    $"{person.DisplayName} has a death date before the birth date. Review the source records; no data was changed.",
                    person.Id));
            }
        }
    }

    private void CheckRelationshipStructure(
        IReadOnlyDictionary<Guid, Person> peopleById,
        IEnumerable<ParentChildRelationship> relationships,
        ICollection<TreeIntegrityIssue> issues)
    {
        foreach (var relationship in relationships)
        {
            if (relationship.ParentPersonId == relationship.ChildPersonId)
            {
                issues.Add(new TreeIntegrityIssue(
                    TreeIntegrityIssueCode.SelfParentRelationship,
                    TreeIntegrityIssueSeverity.Error,
                    "A person is recorded as their own parent. Review the relationship; no data was changed.",
                    relationship.ParentPersonId,
                    relationship.ChildPersonId,
                    relationship.Id));
                continue;
            }

            var hasParent = peopleById.TryGetValue(
                relationship.ParentPersonId,
                out var parent);
            var hasChild = peopleById.TryGetValue(
                relationship.ChildPersonId,
                out var child);

            if (!hasParent || !hasChild)
            {
                var missingId = !hasParent
                    ? relationship.ParentPersonId
                    : relationship.ChildPersonId;

                issues.Add(new TreeIntegrityIssue(
                    TreeIntegrityIssueCode.MissingPersonReference,
                    TreeIntegrityIssueSeverity.Error,
                    $"A parent-child relationship references missing person ID {missingId}. Review the import or relationship record; no data was changed.",
                    hasParent ? relationship.ParentPersonId : relationship.ChildPersonId,
                    hasParent ? relationship.ChildPersonId : relationship.ParentPersonId,
                    relationship.Id));
                continue;
            }

            if (relationship.RelationshipType != ParentChildRelationshipType.Biological)
            {
                continue;
            }

            CheckBiologicalRelationship(parent!, child!, relationship, issues);
        }
    }

    private void CheckBiologicalRelationship(
        Person parent,
        Person child,
        ParentChildRelationship relationship,
        ICollection<TreeIntegrityIssue> issues)
    {
        if (parent.BirthDate.HasValue && child.BirthDate.HasValue)
        {
            if (parent.BirthDate.Value > child.BirthDate.Value)
            {
                issues.Add(new TreeIntegrityIssue(
                    TreeIntegrityIssueCode.ParentBornAfterChild,
                    TreeIntegrityIssueSeverity.Error,
                    $"{parent.DisplayName} is recorded as a biological parent of {child.DisplayName}, but the parent's birth date is after the child's birth date. Review the source records; no data was changed.",
                    parent.Id,
                    child.Id,
                    relationship.Id));
            }
            else
            {
                var parentAge = CalculateAgeOnDate(
                    parent.BirthDate.Value,
                    child.BirthDate.Value);

                if (parentAge < _options.MinimumPlausibleParentAgeYears)
                {
                    issues.Add(new TreeIntegrityIssue(
                        TreeIntegrityIssueCode.ParentTooYoungAtChildBirth,
                        TreeIntegrityIssueSeverity.Error,
                        $"{parent.DisplayName} would have been about {parentAge} years old when {child.DisplayName} was born. Review the relationship and dates; no data was changed.",
                        parent.Id,
                        child.Id,
                        relationship.Id));
                }
                else if (parentAge > _options.UnusuallyHighParentAgeYears)
                {
                    issues.Add(new TreeIntegrityIssue(
                        TreeIntegrityIssueCode.ParentUnusuallyOldAtChildBirth,
                        TreeIntegrityIssueSeverity.Warning,
                        $"{parent.DisplayName} would have been about {parentAge} years old when {child.DisplayName} was born. This may be valid, but should be reviewed against the sources.",
                        parent.Id,
                        child.Id,
                        relationship.Id));
                }
            }
        }

        if (!parent.DeathDate.HasValue || !child.BirthDate.HasValue)
        {
            return;
        }

        if (child.BirthDate.Value <= parent.DeathDate.Value)
        {
            return;
        }

        var latestPlausiblePosthumousBirth = parent.DeathDate.Value.AddDays(
            _options.PosthumousBirthGraceDays);

        if (child.BirthDate.Value > latestPlausiblePosthumousBirth)
        {
            issues.Add(new TreeIntegrityIssue(
                TreeIntegrityIssueCode.ChildBornLongAfterParentDeath,
                TreeIntegrityIssueSeverity.Error,
                $"{child.DisplayName} is recorded as born more than {_options.PosthumousBirthGraceDays} days after biological parent {parent.DisplayName} died. Review the dates and relationship; no data was changed.",
                parent.Id,
                child.Id,
                relationship.Id));
        }
        else
        {
            issues.Add(new TreeIntegrityIssue(
                TreeIntegrityIssueCode.PosthumousBirthForReview,
                TreeIntegrityIssueSeverity.Information,
                $"{child.DisplayName} is recorded as born after biological parent {parent.DisplayName} died. The interval is within the configured posthumous-birth review window, so the record is flagged for human review only.",
                parent.Id,
                child.Id,
                relationship.Id));
        }
    }

    private static void CheckDuplicateAndConflictingRelationships(
        IEnumerable<ParentChildRelationship> relationships,
        ICollection<TreeIntegrityIssue> issues)
    {
        foreach (var pairGroup in relationships.GroupBy(relationship => new
                 {
                     relationship.ParentPersonId,
                     relationship.ChildPersonId
                 }))
        {
            var byType = pairGroup
                .GroupBy(relationship => relationship.RelationshipType)
                .ToArray();

            foreach (var duplicateGroup in byType.Where(group => group.Count() > 1))
            {
                var duplicate = duplicateGroup.Skip(1).First();
                issues.Add(new TreeIntegrityIssue(
                    TreeIntegrityIssueCode.DuplicateParentChildRelationship,
                    TreeIntegrityIssueSeverity.Warning,
                    $"The same {duplicate.RelationshipType} parent-child relationship is recorded more than once. Review before removing anything; no data was changed.",
                    duplicate.ParentPersonId,
                    duplicate.ChildPersonId,
                    duplicate.Id));
            }

            if (byType.Length > 1)
            {
                var relationship = pairGroup.First();
                var types = string.Join(
                    ", ",
                    byType.Select(group => group.Key).OrderBy(value => value));

                issues.Add(new TreeIntegrityIssue(
                    TreeIntegrityIssueCode.ConflictingParentChildRelationshipTypes,
                    TreeIntegrityIssueSeverity.Warning,
                    $"The same parent-child pair has multiple relationship types ({types}). This may be intentional, but should be reviewed.",
                    relationship.ParentPersonId,
                    relationship.ChildPersonId,
                    relationship.Id));
            }
        }
    }

    private static int CalculateAgeOnDate(DateOnly birthDate, DateOnly eventDate)
    {
        var age = eventDate.Year - birthDate.Year;
        if (eventDate < birthDate.AddYears(age))
        {
            age--;
        }

        return age;
    }
}
