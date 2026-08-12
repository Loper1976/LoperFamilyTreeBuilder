using LoperFamilyTreeBuilder.Core.Entities;
using LoperFamilyTreeBuilder.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace LoperFamilyTreeBuilder.Data.Services;

public sealed class PersonProfileQueryService(
    IDbContextFactory<FamilyTreeDbContext> contextFactory)
{
    public async Task<PersonProfileSnapshot?> GetAsync(
        Guid personId,
        CancellationToken cancellationToken = default)
    {
        await using var db =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        var person = await db.People
            .AsNoTracking()
            .Where(candidate => candidate.Id == personId)
            .Select(candidate => new
            {
                candidate.Id,
                candidate.GivenName,
                candidate.MiddleName,
                candidate.Surname,
                candidate.Suffix,
                candidate.BirthDate,
                candidate.DeathDate,
                candidate.IsLiving,
                candidate.CreatedUtc,
                candidate.ModifiedUtc
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (person is null)
        {
            return null;
        }

        var legacyNumber = await db.PersonIdentifiers
            .AsNoTracking()
            .Where(identifier =>
                identifier.PersonId == personId &&
                identifier.IdentifierType == PersonIdentifierType.LegacyNumber)
            .Select(identifier => identifier.Value)
            .SingleOrDefaultAsync(cancellationToken);

        var branches = await db.BranchMemberships
            .AsNoTracking()
            .Where(membership => membership.PersonId == personId)
            .OrderByDescending(membership => membership.IsPrimary)
            .ThenBy(membership => membership.FamilyBranch.Name)
            .Select(membership => membership.FamilyBranch.Name)
            .ToListAsync(cancellationToken);

        var relationships = await db.ParentChildRelationships
            .AsNoTracking()
            .Where(relationship =>
                relationship.ParentPersonId == personId ||
                relationship.ChildPersonId == personId)
            .Select(relationship => new RelationshipRow(
                relationship.ParentPersonId,
                relationship.ChildPersonId,
                relationship.RelationshipType))
            .ToListAsync(cancellationToken);

        var relativeIds = relationships
            .Select(relationship =>
                relationship.ParentPersonId == personId
                    ? relationship.ChildPersonId
                    : relationship.ParentPersonId)
            .Distinct()
            .ToList();

        List<RelativePersonRow> relativeRows;
        Dictionary<Guid, string> relativeLegacyNumbers;

        if (relativeIds.Count == 0)
        {
            relativeRows = [];
            relativeLegacyNumbers = [];
        }
        else
        {
            relativeRows = await db.People
                .AsNoTracking()
                .Where(relative => relativeIds.Contains(relative.Id))
                .Select(relative => new RelativePersonRow(
                    relative.Id,
                    relative.GivenName,
                    relative.MiddleName,
                    relative.Surname,
                    relative.Suffix))
                .ToListAsync(cancellationToken);

            relativeLegacyNumbers = await db.PersonIdentifiers
                .AsNoTracking()
                .Where(identifier =>
                    relativeIds.Contains(identifier.PersonId) &&
                    identifier.IdentifierType == PersonIdentifierType.LegacyNumber)
                .ToDictionaryAsync(
                    identifier => identifier.PersonId,
                    identifier => identifier.Value,
                    cancellationToken);
        }

        var relativesById = relativeRows.ToDictionary(relative => relative.Id);

        var parents = relationships
            .Where(relationship => relationship.ChildPersonId == personId)
            .Select(relationship => CreateRelative(
                relationship.ParentPersonId,
                relationship.RelationshipType,
                relativesById,
                relativeLegacyNumbers))
            .Where(relative => relative is not null)
            .Cast<PersonProfileRelative>()
            .OrderBy(relative => relative.DisplayName)
            .ToList();

        var children = relationships
            .Where(relationship => relationship.ParentPersonId == personId)
            .Select(relationship => CreateRelative(
                relationship.ChildPersonId,
                relationship.RelationshipType,
                relativesById,
                relativeLegacyNumbers))
            .Where(relative => relative is not null)
            .Cast<PersonProfileRelative>()
            .OrderBy(relative => relative.DisplayName)
            .ToList();

        return new PersonProfileSnapshot(
            person.Id,
            person.GivenName,
            person.MiddleName,
            person.Surname,
            person.Suffix,
            person.BirthDate,
            person.DeathDate,
            person.IsLiving,
            legacyNumber,
            branches,
            parents,
            children,
            person.CreatedUtc,
            person.ModifiedUtc);
    }

    private static PersonProfileRelative? CreateRelative(
        Guid relativeId,
        ParentChildRelationshipType relationshipType,
        IReadOnlyDictionary<Guid, RelativePersonRow> relativesById,
        IReadOnlyDictionary<Guid, string> legacyNumbers)
    {
        if (!relativesById.TryGetValue(relativeId, out var relative))
        {
            return null;
        }

        legacyNumbers.TryGetValue(relativeId, out var legacyNumber);

        return new PersonProfileRelative(
            relative.Id,
            relative.DisplayName,
            legacyNumber,
            FormatRelationshipType(relationshipType));
    }

    private static string FormatRelationshipType(
        ParentChildRelationshipType relationshipType) => relationshipType switch
    {
        ParentChildRelationshipType.Biological => "Biological",
        ParentChildRelationshipType.Adoptive => "Adoptive",
        ParentChildRelationshipType.Step => "Step",
        ParentChildRelationshipType.Foster => "Foster",
        ParentChildRelationshipType.Guardian => "Guardian",
        _ => "Custom"
    };

    private sealed record RelationshipRow(
        Guid ParentPersonId,
        Guid ChildPersonId,
        ParentChildRelationshipType RelationshipType);

    private sealed record RelativePersonRow(
        Guid Id,
        string GivenName,
        string MiddleName,
        string Surname,
        string Suffix)
    {
        public string DisplayName => string.Join(
            " ",
            new[] { GivenName, MiddleName, Surname, Suffix }
                .Where(value => !string.IsNullOrWhiteSpace(value)));
    }
}
