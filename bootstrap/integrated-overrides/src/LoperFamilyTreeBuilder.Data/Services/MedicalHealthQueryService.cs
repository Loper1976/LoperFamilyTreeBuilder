using LoperFamilyTreeBuilder.Core.Entities;
using LoperFamilyTreeBuilder.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace LoperFamilyTreeBuilder.Data.Services;

public sealed class MedicalHealthQueryService(
    IDbContextFactory<FamilyTreeDbContext> contextFactory)
{
    public async Task<MedicalHealthDashboard> GetDashboardAsync(
        MedicalAccessScope accessScope,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var query = ApplyAccessFilter(db.MedicalConditions.AsNoTracking(), accessScope);

        var visibleRecords = await query.CountAsync(cancellationToken);
        var peopleWithRecords = await query.Select(record => record.PersonId).Distinct().CountAsync(cancellationToken);
        var hereditary = await query.CountAsync(record => record.IsHereditaryRelevant, cancellationToken);
        var activeOrConfirmed = await query.CountAsync(
            record => record.Status == MedicalConditionStatus.Active ||
                      record.Status == MedicalConditionStatus.Confirmed,
            cancellationToken);

        var patterns = await query
            .Where(record => record.IsHereditaryRelevant)
            .GroupBy(record => record.ConditionName)
            .Select(group => new MedicalFamilyPattern(
                group.Key,
                group.Select(record => record.PersonId).Distinct().Count(),
                group.Count()))
            .OrderByDescending(pattern => pattern.PeopleCount)
            .ThenBy(pattern => pattern.ConditionName)
            .Take(12)
            .ToListAsync(cancellationToken);

        return new MedicalHealthDashboard(
            visibleRecords,
            peopleWithRecords,
            hereditary,
            activeOrConfirmed,
            patterns);
    }

    public async Task<IReadOnlyList<MedicalConditionListItem>> SearchAsync(
        string? searchText,
        MedicalAccessScope accessScope,
        bool hereditaryOnly = false,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var query = ApplyAccessFilter(db.MedicalConditions.AsNoTracking(), accessScope);

        if (hereditaryOnly)
            query = query.Where(record => record.IsHereditaryRelevant);

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var search = searchText.Trim();
            query = query.Where(record =>
                record.ConditionName.Contains(search) ||
                (record.Person != null &&
                    (record.Person.GivenName.Contains(search) ||
                     record.Person.MiddleName.Contains(search) ||
                     record.Person.Surname.Contains(search))));
        }

        return await query
            .OrderBy(record => record.ConditionName)
            .ThenBy(record => record.Person!.Surname)
            .ThenBy(record => record.Person!.GivenName)
            .Take(250)
            .Select(record => new MedicalConditionListItem(
                record.Id,
                record.PersonId,
                (record.Person == null ? "Unknown person" :
                    record.Person.GivenName + " " + record.Person.MiddleName + " " +
                    record.Person.Surname + " " + record.Person.Suffix),
                record.Person == null
                    ? null
                    : record.Person.Identifiers
                        .Where(identifier => identifier.IdentifierType == PersonIdentifierType.LegacyNumber)
                        .Select(identifier => identifier.Value)
                        .FirstOrDefault(),
                record.ConditionName,
                record.Status,
                record.Severity,
                record.IsHereditaryRelevant,
                record.DiagnosisDate,
                record.OnsetAgeYears,
                record.Visibility))
            .ToListAsync(cancellationToken);
    }

    private static IQueryable<MedicalCondition> ApplyAccessFilter(
        IQueryable<MedicalCondition> query,
        MedicalAccessScope accessScope)
    {
        return accessScope switch
        {
            MedicalAccessScope.OwnerAdmin => query,
            MedicalAccessScope.MedicalAuthorized => query.Where(record => record.Visibility != MedicalRecordVisibility.OwnerOnly),
            MedicalAccessScope.FamilySummary => query.Where(record => record.Visibility == MedicalRecordVisibility.FamilySummary),
            _ => query.Where(_ => false)
        };
    }
}
