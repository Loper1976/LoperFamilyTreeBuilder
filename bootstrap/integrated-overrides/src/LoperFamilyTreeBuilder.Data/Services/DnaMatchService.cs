using LoperFamilyTreeBuilder.Core.Entities;
using LoperFamilyTreeBuilder.Core.Genealogy;
using LoperFamilyTreeBuilder.Core.Models;
using LoperFamilyTreeBuilder.Core.Policies;
using Microsoft.EntityFrameworkCore;

namespace LoperFamilyTreeBuilder.Data.Services;

public sealed class DnaMatchService(
    IDbContextFactory<FamilyTreeDbContext> contextFactory,
    DnaClusterEngine clusterEngine)
{
    public async Task<IReadOnlyList<DnaMatchListItem>> GetMatchesAsync(
        DnaAccessScope accessScope,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await ApplyAccessFilter(db.DnaMatches.AsNoTracking(), accessScope)
            .OrderByDescending(match => match.TotalCentimorgans)
            .ThenBy(match => match.DisplayName)
            .Select(match => new DnaMatchListItem(
                match.Id,
                match.ProviderName,
                match.ExternalMatchId,
                match.DisplayName,
                match.TotalCentimorgans,
                match.SharedSegments,
                match.Visibility,
                match.ReviewStatus,
                match.ManualAncestralLineLabel,
                match.ResearchNotes,
                match.ModifiedUtc))
            .Take(5000)
            .ToListAsync(cancellationToken);
    }

    public async Task<DnaClusterResult> GetClustersAsync(
        DnaAccessScope accessScope,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var matches = await ApplyAccessFilter(db.DnaMatches.AsNoTracking(), accessScope)
            .Select(match => new DnaMatchSnapshot(
                match.Id,
                match.DisplayName,
                match.TotalCentimorgans,
                match.ManualAncestralLineLabel))
            .Take(5000)
            .ToListAsync(cancellationToken);

        var visibleIds = matches.Select(match => match.Id).ToList();
        List<DnaSharedMatchSnapshot> edges;
        if (visibleIds.Count == 0)
        {
            edges = [];
        }
        else
        {
            edges = await db.DnaSharedMatches.AsNoTracking()
                .Where(edge => visibleIds.Contains(edge.MatchAId) && visibleIds.Contains(edge.MatchBId))
                .Select(edge => new DnaSharedMatchSnapshot(edge.MatchAId, edge.MatchBId))
                .Take(50000)
                .ToListAsync(cancellationToken);
        }

        return clusterEngine.Build(matches, edges);
    }

    public async Task<DnaClusterDashboard> GetDashboardAsync(
        DnaAccessScope accessScope,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var visibleMatches = await ApplyAccessFilter(db.DnaMatches.AsNoTracking(), accessScope)
            .Select(match => new
            {
                match.Id,
                match.ReviewStatus
            })
            .Take(5000)
            .ToListAsync(cancellationToken);

        var visibleIds = visibleMatches.Select(match => match.Id).ToList();
        var links = 0;
        if (visibleIds.Count > 0)
        {
            links = await db.DnaSharedMatches.AsNoTracking()
                .CountAsync(edge => visibleIds.Contains(edge.MatchAId) && visibleIds.Contains(edge.MatchBId), cancellationToken);
        }

        var clusters = await GetClustersAsync(accessScope, cancellationToken);
        return new DnaClusterDashboard(
            visibleMatches.Count,
            links,
            clusters.Clusters.Count,
            visibleMatches.Count(match => match.ReviewStatus == DnaMatchReviewStatus.Reviewed),
            clusters.UnclusteredMatches.Count);
    }

    public async Task<Guid> AddMatchAsync(
        DnaMatchImportRow row,
        DnaAccessScope accessScope,
        CancellationToken cancellationToken = default)
    {
        RequireEditAccess(accessScope);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        var provider = row.ProviderName.Trim();
        var externalId = row.ExternalMatchId.Trim();
        if (await db.DnaMatches.AnyAsync(match =>
                match.ProviderName == provider && match.ExternalMatchId == externalId,
                cancellationToken))
        {
            throw new InvalidOperationException("That provider match identifier already exists. Existing DNA matches are never silently merged or replaced.");
        }

        var match = new DnaMatch(
            row.ProviderName,
            row.ExternalMatchId,
            row.DisplayName,
            row.TotalCentimorgans,
            row.SharedSegments);
        db.DnaMatches.Add(match);
        await db.SaveChangesAsync(cancellationToken);
        return match.Id;
    }

    public async Task<DnaImportResult> ImportAsync(
        IReadOnlyCollection<DnaMatchImportRow> rows,
        DnaAccessScope accessScope,
        CancellationToken cancellationToken = default)
    {
        RequireEditAccess(accessScope);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        var existingKeys = (await db.DnaMatches.AsNoTracking()
                .Select(match => new { match.ProviderName, match.ExternalMatchId })
                .ToListAsync(cancellationToken))
            .Select(match => Key(match.ProviderName, match.ExternalMatchId))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var addedIds = new List<Guid>();
        var duplicateRows = 0;
        foreach (var row in rows)
        {
            var key = Key(row.ProviderName, row.ExternalMatchId);
            if (!existingKeys.Add(key))
            {
                duplicateRows++;
                continue;
            }

            var match = new DnaMatch(
                row.ProviderName,
                row.ExternalMatchId,
                row.DisplayName,
                row.TotalCentimorgans,
                row.SharedSegments);
            db.DnaMatches.Add(match);
            addedIds.Add(match.Id);
        }

        await db.SaveChangesAsync(cancellationToken);
        return new DnaImportResult(addedIds.Count, duplicateRows, addedIds);
    }

    public async Task<bool> AddSharedMatchAsync(
        Guid firstMatchId,
        Guid secondMatchId,
        string? evidenceSource,
        DnaAccessScope accessScope,
        CancellationToken cancellationToken = default)
    {
        RequireEditAccess(accessScope);
        var edge = new DnaSharedMatch(firstMatchId, secondMatchId, evidenceSource);

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var existingCount = await db.DnaMatches.CountAsync(match =>
            match.Id == edge.MatchAId || match.Id == edge.MatchBId,
            cancellationToken);
        if (existingCount != 2)
            throw new InvalidOperationException("Both DNA matches must exist before shared-match evidence can be recorded.");

        if (await db.DnaSharedMatches.AnyAsync(existing =>
                existing.MatchAId == edge.MatchAId && existing.MatchBId == edge.MatchBId,
                cancellationToken))
        {
            return false;
        }

        db.DnaSharedMatches.Add(edge);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task SaveReviewAsync(
        Guid matchId,
        string? manualAncestralLineLabel,
        string? researchNotes,
        DnaMatchVisibility visibility,
        DnaAccessScope accessScope,
        CancellationToken cancellationToken = default)
    {
        RequireEditAccess(accessScope);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var match = await db.DnaMatches.SingleOrDefaultAsync(item => item.Id == matchId, cancellationToken)
            ?? throw new InvalidOperationException("The DNA match was not found.");

        match.SaveResearchReview(manualAncestralLineLabel, researchNotes);
        match.SetVisibility(visibility);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<DnaMatch> ApplyAccessFilter(
        IQueryable<DnaMatch> query,
        DnaAccessScope accessScope)
    {
        return accessScope switch
        {
            DnaAccessScope.OwnerAdmin => query,
            DnaAccessScope.DnaAuthorized => query.Where(match => match.Visibility == DnaMatchVisibility.DnaAuthorized),
            _ => query.Where(_ => false)
        };
    }

    private static void RequireEditAccess(DnaAccessScope accessScope)
    {
        if (!DnaPrivacyPolicy.CanEdit(accessScope))
            throw new UnauthorizedAccessException("Owner/admin access is required to modify DNA match records.");
    }

    private static string Key(string providerName, string externalMatchId) =>
        $"{providerName.Trim()}\u001f{externalMatchId.Trim()}";
}
