using LoperFamilyTreeBuilder.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace LoperFamilyTreeBuilder.Data.Services;

public sealed record RelationshipWorkspaceRow(Guid PersonId, string DisplayName, string LegacyNumber, string Role, string RelationshipType);
public sealed record UnionWorkspaceRow(Guid Id, Guid OtherPersonId, string OtherPersonName, string OtherLegacyNumber, FamilyUnionType UnionType, FamilyUnionStatus Status, DateOnly? StartDate, DateOnly? EndDate, string PlaceText, string SourceCitation);
public sealed record LifeEventRow(Guid Id, LifeEventType EventType, string Title, DateOnly? StartDate, DateOnly? EndDate, bool IsApproximate, string PlaceText, decimal? Latitude, decimal? Longitude, string Notes, string SourceCitation);
public sealed record MapPointRow(Guid EventId, string Title, string EventType, DateOnly? Date, string PlaceText, decimal? Latitude, decimal? Longitude, bool IsApproximate, string SourceCitation);
public sealed record ArchiveItemRow(Guid Id, ArchiveItemType ItemType, Guid? PersonId, string PersonName, string Title, string OriginalPath, string Sha256, DateTimeOffset? CapturedUtc, string PlaceText, decimal? Latitude, decimal? Longitude, string Caption, string Provenance, Guid? SourceRecordId);
public sealed record SourceRecordRow(Guid Id, Guid? PersonId, string PersonName, string Title, string Citation, string Repository, string CallNumberOrUrl, string Notes);

public sealed class LifeArchiveService(IDbContextFactory<FamilyTreeDbContext> contextFactory)
{
    public async Task<IReadOnlyList<RelationshipWorkspaceRow>> GetParentChildRelationshipsAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var links = await db.ParentChildRelationships.AsNoTracking()
            .Where(x => x.ParentPersonId == personId || x.ChildPersonId == personId)
            .Select(x => new { x.ParentPersonId, x.ChildPersonId, x.RelationshipType })
            .ToListAsync(cancellationToken);
        var ids = links.Select(x => x.ParentPersonId == personId ? x.ChildPersonId : x.ParentPersonId).Distinct().ToList();
        var names = await GetPersonNamesAsync(db, ids, cancellationToken);
        var legacy = await GetLegacyNumbersAsync(db, ids, cancellationToken);
        return links.Select(x =>
        {
            var relativeId = x.ParentPersonId == personId ? x.ChildPersonId : x.ParentPersonId;
            var role = x.ParentPersonId == personId ? "Child" : "Parent";
            return new RelationshipWorkspaceRow(relativeId, names.GetValueOrDefault(relativeId, "Unknown person"), legacy.GetValueOrDefault(relativeId, string.Empty), role, x.RelationshipType.ToString());
        }).OrderBy(x => x.Role).ThenBy(x => x.DisplayName).ToList();
    }

    public async Task<IReadOnlyList<UnionWorkspaceRow>> GetUnionsAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var unions = await db.FamilyUnions.AsNoTracking().Where(x => x.Person1Id == personId || x.Person2Id == personId).ToListAsync(cancellationToken);
        var ids = unions.Select(x => x.Person1Id == personId ? x.Person2Id : x.Person1Id).Distinct().ToList();
        var names = await GetPersonNamesAsync(db, ids, cancellationToken);
        var legacy = await GetLegacyNumbersAsync(db, ids, cancellationToken);
        return unions.Select(x =>
        {
            var otherId = x.Person1Id == personId ? x.Person2Id : x.Person1Id;
            return new UnionWorkspaceRow(x.Id, otherId, names.GetValueOrDefault(otherId, "Unknown person"), legacy.GetValueOrDefault(otherId, string.Empty), x.UnionType, x.Status, x.StartDate, x.EndDate, x.PlaceText, x.SourceCitation);
        }).OrderBy(x => x.StartDate).ThenBy(x => x.OtherPersonName).ToList();
    }

    public async Task<Guid> AddUnionAsync(Guid person1Id, Guid person2Id, FamilyUnionType unionType, FamilyUnionStatus status, DateOnly? startDate, DateOnly? endDate, string? placeText, string? notes, string? sourceCitation, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (!await db.People.AnyAsync(x => x.Id == person1Id, cancellationToken) || !await db.People.AnyAsync(x => x.Id == person2Id, cancellationToken))
            throw new InvalidOperationException("Both people must exist before a union can be recorded.");
        var exists = await db.FamilyUnions.AnyAsync(x => (x.Person1Id == person1Id && x.Person2Id == person2Id) || (x.Person1Id == person2Id && x.Person2Id == person1Id), cancellationToken);
        if (exists) throw new InvalidOperationException("A union between these people is already recorded.");
        var union = new FamilyUnion(person1Id, person2Id, unionType);
        union.Update(status, startDate, endDate, placeText, notes, sourceCitation);
        db.FamilyUnions.Add(union);
        await db.SaveChangesAsync(cancellationToken);
        return union.Id;
    }

    public async Task<IReadOnlyList<LifeEventRow>> GetTimelineAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.LifeEvents.AsNoTracking().Where(x => x.PersonId == personId)
            .OrderBy(x => x.StartDate).ThenBy(x => x.EventType).ThenBy(x => x.Title)
            .Select(x => new LifeEventRow(x.Id, x.EventType, x.Title, x.StartDate, x.EndDate, x.IsDateApproximate, x.OriginalPlaceText, x.Latitude, x.Longitude, x.Notes, x.SourceCitation))
            .ToListAsync(cancellationToken);
    }

    public async Task<Guid> AddLifeEventAsync(Guid personId, LifeEventType eventType, string title, DateOnly? startDate, DateOnly? endDate, bool approximate, string? placeText, decimal? latitude, decimal? longitude, string? notes, string? sourceCitation, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (!await db.People.AnyAsync(x => x.Id == personId, cancellationToken)) throw new InvalidOperationException("Person was not found.");
        var item = new LifeEvent(personId, eventType, title);
        item.UpdateChronology(startDate, endDate, approximate);
        item.UpdateLocation(placeText, latitude, longitude);
        item.UpdateEvidence(notes, sourceCitation);
        db.LifeEvents.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return item.Id;
    }

    public async Task<IReadOnlyList<MapPointRow>> GetMapPointsAsync(Guid personId, CancellationToken cancellationToken = default)
    {
        var events = await GetTimelineAsync(personId, cancellationToken);
        return events.Where(x => !string.IsNullOrWhiteSpace(x.PlaceText) || (x.Latitude.HasValue && x.Longitude.HasValue))
            .Select(x => new MapPointRow(x.Id, x.Title, x.EventType.ToString(), x.StartDate, x.PlaceText, x.Latitude, x.Longitude, x.IsApproximate, x.SourceCitation)).ToList();
    }

    public async Task<IReadOnlyList<ArchiveItemRow>> GetArchiveItemsAsync(ArchiveItemType? itemType = null, Guid? personId = null, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var query = db.ArchiveItems.AsNoTracking().AsQueryable();
        if (itemType.HasValue) query = query.Where(x => x.ItemType == itemType.Value);
        if (personId.HasValue) query = query.Where(x => x.PersonId == personId.Value);
        var items = await query.OrderByDescending(x => x.CapturedUtc).ThenBy(x => x.Title).ToListAsync(cancellationToken);
        var ids = items.Where(x => x.PersonId.HasValue).Select(x => x.PersonId!.Value).Distinct().ToList();
        var names = await GetPersonNamesAsync(db, ids, cancellationToken);
        return items.Select(x => new ArchiveItemRow(x.Id, x.ItemType, x.PersonId, x.PersonId.HasValue ? names.GetValueOrDefault(x.PersonId.Value, "Unknown person") : string.Empty, x.Title, x.OriginalPath, x.Sha256, x.CapturedUtc, x.OriginalPlaceText, x.Latitude, x.Longitude, x.Caption, x.Provenance, x.SourceRecordId)).ToList();
    }

    public async Task<Guid> AddArchiveItemAsync(ArchiveItemType itemType, string title, string originalPath, Guid? personId, Guid? sourceRecordId, string? sha256, DateTimeOffset? capturedUtc, string? placeText, decimal? latitude, decimal? longitude, string? caption, string? provenance, string? metadataJson, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (personId.HasValue && !await db.People.AnyAsync(x => x.Id == personId.Value, cancellationToken)) throw new InvalidOperationException("Person was not found.");
        if (sourceRecordId.HasValue && !await db.SourceRecords.AnyAsync(x => x.Id == sourceRecordId.Value, cancellationToken)) throw new InvalidOperationException("Source was not found.");
        var item = new ArchiveItem(itemType, title, originalPath);
        item.LinkPerson(personId);
        item.LinkSource(sourceRecordId);
        item.UpdateMetadata(sha256, capturedUtc, placeText, latitude, longitude, caption, provenance, metadataJson);
        db.ArchiveItems.Add(item);
        await db.SaveChangesAsync(cancellationToken);
        return item.Id;
    }

    public async Task<IReadOnlyList<SourceRecordRow>> GetSourcesAsync(Guid? personId = null, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var query = db.SourceRecords.AsNoTracking().AsQueryable();
        if (personId.HasValue) query = query.Where(x => x.PersonId == personId.Value);
        var sources = await query.OrderBy(x => x.Title).ToListAsync(cancellationToken);
        var ids = sources.Where(x => x.PersonId.HasValue).Select(x => x.PersonId!.Value).Distinct().ToList();
        var names = await GetPersonNamesAsync(db, ids, cancellationToken);
        return sources.Select(x => new SourceRecordRow(x.Id, x.PersonId, x.PersonId.HasValue ? names.GetValueOrDefault(x.PersonId.Value, "Unknown person") : string.Empty, x.Title, x.Citation, x.Repository, x.CallNumberOrUrl, x.Notes)).ToList();
    }

    public async Task<Guid> AddSourceAsync(string title, string citation, Guid? personId, string? repository, string? callNumberOrUrl, string? notes, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (personId.HasValue && !await db.People.AnyAsync(x => x.Id == personId.Value, cancellationToken)) throw new InvalidOperationException("Person was not found.");
        var source = new SourceRecord(title, citation);
        source.Update(personId, repository, callNumberOrUrl, notes);
        db.SourceRecords.Add(source);
        await db.SaveChangesAsync(cancellationToken);
        return source.Id;
    }

    private static async Task<Dictionary<Guid, string>> GetPersonNamesAsync(FamilyTreeDbContext db, IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0) return [];
        return await db.People.AsNoTracking().Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => ((x.GivenName + " " + x.MiddleName).Trim() + " " + x.Surname + " " + x.Suffix).Trim(), cancellationToken);
    }

    private static async Task<Dictionary<Guid, string>> GetLegacyNumbersAsync(FamilyTreeDbContext db, IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0) return [];
        return await db.PersonIdentifiers.AsNoTracking().Where(x => ids.Contains(x.PersonId) && x.IdentifierType == PersonIdentifierType.LegacyNumber).ToDictionaryAsync(x => x.PersonId, x => x.Value, cancellationToken);
    }
}
