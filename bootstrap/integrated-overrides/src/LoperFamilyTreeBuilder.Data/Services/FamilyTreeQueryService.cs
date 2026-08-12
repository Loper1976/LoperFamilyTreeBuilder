using LoperFamilyTreeBuilder.Core.Entities;
using LoperFamilyTreeBuilder.Core.Genealogy;
using LoperFamilyTreeBuilder.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace LoperFamilyTreeBuilder.Data.Services;

public sealed class FamilyTreeQueryService(
    IDbContextFactory<FamilyTreeDbContext> contextFactory,
    FamilyTreeGraphBuilder graphBuilder)
{
    public async Task<FamilyTreeView> GetViewAsync(Guid rootPersonId, FamilyTreeDirection direction, int maxDepth = 4, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var personRows = await db.People.AsNoTracking().Select(person => new
        {
            person.Id, person.GivenName, person.MiddleName, person.Surname, person.Suffix,
            person.BirthDate, person.DeathDate, person.IsLiving,
            LegacyNumber = person.Identifiers.Where(identifier => identifier.IdentifierType == PersonIdentifierType.LegacyNumber).Select(identifier => identifier.Value).FirstOrDefault()
        }).ToListAsync(cancellationToken);

        var people = personRows.Select(person => new FamilyTreePersonSnapshot(
            person.Id,
            string.Join(" ", new[] { person.GivenName, person.MiddleName, person.Surname, person.Suffix }.Where(value => !string.IsNullOrWhiteSpace(value))),
            person.BirthDate, person.DeathDate, person.IsLiving, person.LegacyNumber)).ToList();

        var relationships = await db.ParentChildRelationships.AsNoTracking()
            .Select(relationship => new FamilyTreeRelationshipSnapshot(relationship.ParentPersonId, relationship.ChildPersonId, relationship.RelationshipType))
            .ToListAsync(cancellationToken);

        return graphBuilder.Build(rootPersonId, direction, maxDepth, people, relationships);
    }
}
