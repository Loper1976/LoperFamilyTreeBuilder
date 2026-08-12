using LoperFamilyTreeBuilder.Core.Validation;
using Microsoft.EntityFrameworkCore;

namespace LoperFamilyTreeBuilder.Data.Services;

public sealed class TreeIntegrityQueryService(
    IDbContextFactory<FamilyTreeDbContext> contextFactory)
{
    private readonly TreeIntegrityChecker _checker = new();

    public async Task<IReadOnlyList<TreeIntegrityIssue>> CheckAsync(
        CancellationToken cancellationToken = default)
    {
        await using var db =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        var people = await db.People
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var relationships = await db.ParentChildRelationships
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return _checker.Check(people, relationships);
    }
}
