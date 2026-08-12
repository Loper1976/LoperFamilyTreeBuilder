using LoperFamilyTreeBuilder.Core.Entities;
using LoperFamilyTreeBuilder.Core.Validation;

namespace LoperFamilyTreeBuilder.Tests;

public sealed class TreeIntegrityCheckerTests
{
    private readonly TreeIntegrityChecker _checker = new();

    [Fact]
    public void Check_FlagsDeathBeforeBirthWithoutChangingPerson()
    {
        var person = new Person("Test", "Person");
        person.SetBirthDate(new DateOnly(1950, 1, 1));
        person.SetDeathDate(new DateOnly(1949, 12, 31));
        var beforeBirth = person.BirthDate;
        var beforeDeath = person.DeathDate;

        var issues = _checker.Check(
            new[] { person },
            Array.Empty<ParentChildRelationship>());

        var issue = Assert.Single(issues);
        Assert.Equal(TreeIntegrityIssueCode.DeathBeforeBirth, issue.Code);
        Assert.Equal(TreeIntegrityIssueSeverity.Error, issue.Severity);
        Assert.Equal(beforeBirth, person.BirthDate);
        Assert.Equal(beforeDeath, person.DeathDate);
    }

    [Fact]
    public void Check_FlagsBiologicalParentWhoIsTooYoung()
    {
        var parent = PersonWithBirthDate("Young", "Parent", 2000, 1, 1);
        var child = PersonWithBirthDate("Test", "Child", 2010, 1, 1);
        var relationship = new ParentChildRelationship(
            parent.Id,
            child.Id,
            ParentChildRelationshipType.Biological);

        var issues = _checker.Check(
            new[] { parent, child },
            new[] { relationship });

        Assert.Contains(
            issues,
            issue => issue.Code == TreeIntegrityIssueCode.ParentTooYoungAtChildBirth &&
                     issue.Severity == TreeIntegrityIssueSeverity.Error);
    }

    [Fact]
    public void Check_FlagsChildBornLongAfterBiologicalParentDeath()
    {
        var parent = PersonWithBirthDate("Deceased", "Parent", 1900, 1, 1);
        parent.SetDeathDate(new DateOnly(1930, 1, 1));
        var child = PersonWithBirthDate("Late", "Child", 1931, 1, 1);
        var relationship = new ParentChildRelationship(
            parent.Id,
            child.Id,
            ParentChildRelationshipType.Biological);

        var issues = _checker.Check(
            new[] { parent, child },
            new[] { relationship });

        Assert.Contains(
            issues,
            issue => issue.Code == TreeIntegrityIssueCode.ChildBornLongAfterParentDeath &&
                     issue.Severity == TreeIntegrityIssueSeverity.Error);
    }

    [Fact]
    public void Check_DoesNotApplyBiologicalAgeRulesToAdoptiveRelationships()
    {
        var parent = PersonWithBirthDate("Adoptive", "Parent", 2000, 1, 1);
        var child = PersonWithBirthDate("Adopted", "Child", 2010, 1, 1);
        var relationship = new ParentChildRelationship(
            parent.Id,
            child.Id,
            ParentChildRelationshipType.Adoptive);

        var issues = _checker.Check(
            new[] { parent, child },
            new[] { relationship });

        Assert.DoesNotContain(
            issues,
            issue => issue.Code == TreeIntegrityIssueCode.ParentTooYoungAtChildBirth);
    }

    [Fact]
    public void Check_FlagsDuplicateAndConflictingRelationshipRecords()
    {
        var parent = PersonWithBirthDate("Test", "Parent", 1950, 1, 1);
        var child = PersonWithBirthDate("Test", "Child", 1975, 1, 1);
        var biological1 = new ParentChildRelationship(
            parent.Id,
            child.Id,
            ParentChildRelationshipType.Biological);
        var biological2 = new ParentChildRelationship(
            parent.Id,
            child.Id,
            ParentChildRelationshipType.Biological);
        var adoptive = new ParentChildRelationship(
            parent.Id,
            child.Id,
            ParentChildRelationshipType.Adoptive);

        var issues = _checker.Check(
            new[] { parent, child },
            new[] { biological1, biological2, adoptive });

        Assert.Contains(
            issues,
            issue => issue.Code == TreeIntegrityIssueCode.DuplicateParentChildRelationship);
        Assert.Contains(
            issues,
            issue => issue.Code == TreeIntegrityIssueCode.ConflictingParentChildRelationshipTypes);
    }

    [Fact]
    public void Check_ReturnsNoIssuesForPlausibleFamily()
    {
        var parent = PersonWithBirthDate("Test", "Parent", 1950, 1, 1);
        var child = PersonWithBirthDate("Test", "Child", 1975, 1, 1);
        var relationship = new ParentChildRelationship(
            parent.Id,
            child.Id,
            ParentChildRelationshipType.Biological);

        var issues = _checker.Check(
            new[] { parent, child },
            new[] { relationship });

        Assert.Empty(issues);
    }

    [Fact]
    public void Check_NeverNormalizesOrChangesLegacyNumbers()
    {
        var parent = PersonWithBirthDate("Robert", "Loper", 1927, 7, 30);
        var child = PersonWithBirthDate("Test", "Loper", 1955, 1, 1);
        var legacy = parent.AddLegacyNumber(" 21313.00 ");
        var relationship = new ParentChildRelationship(
            parent.Id,
            child.Id,
            ParentChildRelationshipType.Biological);

        _checker.Check(
            new[] { parent, child },
            new[] { relationship });

        Assert.Equal(" 21313.00 ", legacy.Value);
    }

    private static Person PersonWithBirthDate(
        string givenName,
        string surname,
        int year,
        int month,
        int day)
    {
        var person = new Person(givenName, surname);
        person.SetBirthDate(new DateOnly(year, month, day));
        return person;
    }
}
