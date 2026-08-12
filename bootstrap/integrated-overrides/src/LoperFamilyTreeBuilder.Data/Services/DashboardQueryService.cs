using Microsoft.EntityFrameworkCore;

namespace LoperFamilyTreeBuilder.Data.Services;

public sealed class DashboardQueryService(
    IDbContextFactory<FamilyTreeDbContext> contextFactory)
{
    public async Task<DashboardSummary> GetSummaryAsync(
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        var people = await db.People.CountAsync(cancellationToken);
        var living = await db.People.CountAsync(person => person.IsLiving, cancellationToken);
        var branches = await db.FamilyBranches.CountAsync(cancellationToken);
        var medicalRecords = await db.MedicalConditions.CountAsync(cancellationToken);

        return new DashboardSummary(
            People: people,
            FamilyBranches: branches,
            Living: living,
            Deceased: people - living,
            Photos: 0,
            Documents: 0,
            Sources: 0,
            Cemeteries: 0,
            MedicalRecords: medicalRecords,
            PendingReviews: 0);
    }
}

public sealed record DashboardSummary(
    int People,
    int FamilyBranches,
    int Living,
    int Deceased,
    int Photos,
    int Documents,
    int Sources,
    int Cemeteries,
    int MedicalRecords,
    int PendingReviews);
