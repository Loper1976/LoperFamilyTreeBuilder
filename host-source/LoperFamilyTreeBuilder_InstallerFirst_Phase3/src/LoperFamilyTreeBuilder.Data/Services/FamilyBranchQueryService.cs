using LoperFamilyTreeBuilder.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace LoperFamilyTreeBuilder.Data.Services;

public sealed class FamilyBranchQueryService(
    IDbContextFactory<FamilyTreeDbContext> contextFactory)
{
    public async Task<IReadOnlyList<FamilyBranchListItem>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        await using var db =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        return await db.FamilyBranches
            .AsNoTracking()
            .OrderBy(branch => branch.Name)
            .Select(branch => new FamilyBranchListItem(
                branch.Id,
                branch.Name,
                branch.ShortCode))
            .ToListAsync(cancellationToken);
    }
}
