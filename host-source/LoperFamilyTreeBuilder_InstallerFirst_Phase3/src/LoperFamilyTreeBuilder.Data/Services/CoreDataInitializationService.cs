using LoperFamilyTreeBuilder.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace LoperFamilyTreeBuilder.Data.Services;

public sealed class CoreDataInitializationService(
    IDbContextFactory<FamilyTreeDbContext> contextFactory)
{
    public async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        await using var db =
            await contextFactory.CreateDbContextAsync(cancellationToken);

        if (!await db.FamilyBranches.AnyAsync(cancellationToken))
        {
            db.FamilyBranches.Add(new FamilyBranch("Loper", string.Empty));
            db.FamilyBranches.Add(new FamilyBranch("Beadle", string.Empty));

            db.AuditEvents.Add(
                new AuditEvent(
                    action: "Initialize",
                    entityType: nameof(FamilyBranch),
                    entityId: "system",
                    actor: "System",
                    summary:
                        "Initialized baseline Loper and Beadle family branches without assigning modern branch codes."));

            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
