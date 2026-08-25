using LoperFamilyTreeBuilder.Core.Entities;
using LoperFamilyTreeBuilder.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace LoperFamilyTreeBuilder.Data.Services;

public sealed class PersonProfileQueryService(
    IDbContextFactory<FamilyTreeDbContext> contextFactory)
{
    public async Task<PersonProfileModel?> GetAsync(
        Guid personId,
        CancellationToken cancellationToken = default)
    {
        await using var db =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        var person = await db.People
            .AsNoTracking()
            .Where(item => item.Id == personId)
            .Select(item => new
            {
                item.Id,
                item.GivenName,
                item.MiddleName,
                item.Surname,
                item.Suffix,
                item.BirthDate,
                item.DeathDate,
                item.IsLiving,
                item.CreatedUtc,
                item.ModifiedUtc,
                LoperId = item.Identifiers
                    .Where(identifier =>
                        identifier.IdentifierType == PersonIdentifierType.LoperId)
                    .Select(identifier => identifier.Value)
                    .FirstOrDefault(),
                LegacyNumber = item.Identifiers
                    .Where(identifier =>
                        identifier.IdentifierType ==
                            PersonIdentifierType.LegacyNumber)
                    .Select(identifier => identifier.Value)
                    .FirstOrDefault()
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (person is null)
        {
            return null;
        }

        var branches = await db.BranchMemberships
            .AsNoTracking()
            .Where(membership => membership.PersonId == personId)
            .OrderByDescending(membership => membership.IsPrimary)
            .ThenBy(membership => membership.FamilyBranch.Name)
            .Select(membership => new PersonBranchProfileItem(
                membership.FamilyBranchId,
                membership.FamilyBranch.Name,
                membership.FamilyBranch.ShortCode,
                membership.IsPrimary))
            .ToListAsync(cancellationToken);

        var parentRows = await db.ParentChildRelationships
            .AsNoTracking()
            .Where(relationship => relationship.ChildPersonId == personId)
            .OrderBy(relationship => relationship.ParentPerson.Surname)
            .ThenBy(relationship => relationship.ParentPerson.GivenName)
            .Select(relationship => new
            {
                relationship.ParentPersonId,
                relationship.RelationshipType,
                relationship.ParentPerson.GivenName,
                relationship.ParentPerson.MiddleName,
                relationship.ParentPerson.Surname,
                relationship.ParentPerson.Suffix,
                LegacyNumber = relationship.ParentPerson.Identifiers
                    .Where(identifier =>
                        identifier.IdentifierType ==
                            PersonIdentifierType.LegacyNumber)
                    .Select(identifier => identifier.Value)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var parents = parentRows
            .Select(row => new PersonRelationshipProfileItem(
                row.ParentPersonId,
                BuildDisplayName(
                    row.GivenName,
                    row.MiddleName,
                    row.Surname,
                    row.Suffix),
                row.RelationshipType.ToString(),
                row.LegacyNumber))
            .ToList();

        var childRows = await db.ParentChildRelationships
            .AsNoTracking()
            .Where(relationship => relationship.ParentPersonId == personId)
            .OrderBy(relationship => relationship.ChildPerson.BirthDate)
            .ThenBy(relationship => relationship.ChildPerson.Surname)
            .ThenBy(relationship => relationship.ChildPerson.GivenName)
            .Select(relationship => new
            {
                relationship.ChildPersonId,
                relationship.RelationshipType,
                relationship.ChildPerson.GivenName,
                relationship.ChildPerson.MiddleName,
                relationship.ChildPerson.Surname,
                relationship.ChildPerson.Suffix,
                LegacyNumber = relationship.ChildPerson.Identifiers
                    .Where(identifier =>
                        identifier.IdentifierType ==
                            PersonIdentifierType.LegacyNumber)
                    .Select(identifier => identifier.Value)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        var children = childRows
            .Select(row => new PersonRelationshipProfileItem(
                row.ChildPersonId,
                BuildDisplayName(
                    row.GivenName,
                    row.MiddleName,
                    row.Surname,
                    row.Suffix),
                row.RelationshipType.ToString(),
                row.LegacyNumber))
            .ToList();

        var entityId = personId.ToString();

        var audits = await db.AuditEvents
            .AsNoTracking()
            .Where(audit =>
                audit.EntityType == nameof(Person) &&
                audit.EntityId == entityId)
            .OrderByDescending(audit => audit.OccurredUtc)
            .Take(100)
            .Select(audit => new PersonAuditProfileItem(
                audit.Id,
                audit.OccurredUtc,
                audit.Action,
                audit.Actor,
                audit.Summary,
                audit.PreviousValueJson,
                audit.NewValueJson))
            .ToListAsync(cancellationToken);

        return new PersonProfileModel(
            person.Id,
            person.GivenName,
            person.MiddleName,
            person.Surname,
            person.Suffix,
            person.BirthDate,
            person.DeathDate,
            person.IsLiving,
            person.LoperId,
            person.LegacyNumber,
            person.CreatedUtc,
            person.ModifiedUtc,
            branches,
            parents,
            children,
            audits);
    }

    private static string BuildDisplayName(
        string givenName,
        string middleName,
        string surname,
        string suffix)
    {
        var values = new[]
        {
            givenName,
            middleName,
            surname,
            suffix
        }
        .Where(value => !string.IsNullOrWhiteSpace(value));

        var displayName = string.Join(" ", values);
        return string.IsNullOrWhiteSpace(displayName)
            ? "(Unnamed person)"
            : displayName;
    }
}
