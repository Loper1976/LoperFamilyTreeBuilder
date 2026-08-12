using LoperFamilyTreeBuilder.Core.Entities;
using LoperFamilyTreeBuilder.Core.Models;
using LoperFamilyTreeBuilder.Core.Policies;
using Microsoft.EntityFrameworkCore;

namespace LoperFamilyTreeBuilder.Data.Services;

public sealed class HandwritingTranscriptionService(
    IDbContextFactory<FamilyTreeDbContext> contextFactory)
{
    public async Task<HandwritingTranscriptionDashboard> GetDashboardAsync(
        HandwritingTranscriptionAccessScope accessScope,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var query = ApplyAccessFilter(db.HandwritingTranscriptions.AsNoTracking(), accessScope);

        return new HandwritingTranscriptionDashboard(
            await query.CountAsync(cancellationToken),
            await query.CountAsync(record => record.Status == HandwritingTranscriptionStatus.Queued, cancellationToken),
            await query.CountAsync(record => record.Status == HandwritingTranscriptionStatus.DraftReady, cancellationToken),
            await query.CountAsync(record => record.Status == HandwritingTranscriptionStatus.NeedsReview, cancellationToken),
            await query.CountAsync(record => record.Status == HandwritingTranscriptionStatus.Approved, cancellationToken),
            await query.CountAsync(record => record.Status == HandwritingTranscriptionStatus.Failed, cancellationToken));
    }

    public async Task<IReadOnlyList<HandwritingTranscriptionQueueItem>> GetQueueAsync(
        HandwritingTranscriptionAccessScope accessScope,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var records = await ApplyAccessFilter(db.HandwritingTranscriptions.AsNoTracking(), accessScope)
            .Include(record => record.Person)
                .ThenInclude(person => person!.Identifiers)
            .OrderByDescending(record => record.ModifiedUtc)
            .Take(250)
            .ToListAsync(cancellationToken);

        return records.Select(record => new HandwritingTranscriptionQueueItem(
                record.Id,
                record.PersonId,
                PersonDisplayName(record.Person),
                LegacyNumber(record.Person),
                record.DocumentTitle,
                record.ArchiveRelativePath,
                record.Status,
                record.Visibility,
                record.ProviderName,
                record.ModelName,
                record.Confidence,
                record.ModifiedUtc))
            .ToList();
    }

    public async Task<HandwritingTranscriptionDetail?> GetAsync(
        Guid id,
        HandwritingTranscriptionAccessScope accessScope,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var record = await ApplyAccessFilter(db.HandwritingTranscriptions.AsNoTracking(), accessScope)
            .Include(item => item.Person)
                .ThenInclude(person => person!.Identifiers)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (record is null)
            return null;

        return new HandwritingTranscriptionDetail(
            record.Id,
            record.PersonId,
            PersonDisplayName(record.Person),
            LegacyNumber(record.Person),
            record.DocumentTitle,
            record.ArchiveRelativePath,
            record.OriginalImageHashSha256,
            record.SourceCitation,
            record.Status,
            record.Visibility,
            record.ProviderName,
            record.ModelName,
            record.Confidence,
            record.MachineDraft,
            record.ReviewedTranscript,
            record.ApprovedTranscript,
            record.FailureMessage,
            record.CreatedUtc,
            record.ModifiedUtc,
            record.ApprovedUtc);
    }

    public async Task<Guid> QueueAsync(
        string documentTitle,
        string archiveRelativePath,
        string? sourceCitation,
        Guid? personId,
        HandwritingTranscriptionAccessScope accessScope,
        CancellationToken cancellationToken = default)
    {
        RequireEditAccess(accessScope);

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        if (personId.HasValue &&
            !await db.People.AnyAsync(person => person.Id == personId.Value, cancellationToken))
        {
            throw new InvalidOperationException("The selected person does not exist.");
        }

        var record = new HandwritingTranscription(
            documentTitle,
            archiveRelativePath,
            sourceCitation,
            personId);

        db.HandwritingTranscriptions.Add(record);
        await db.SaveChangesAsync(cancellationToken);
        return record.Id;
    }

    public async Task RecordMachineDraftAsync(
        Guid id,
        string transcript,
        string? providerName,
        string? modelName,
        decimal? confidence,
        HandwritingTranscriptionAccessScope accessScope,
        CancellationToken cancellationToken = default)
    {
        RequireEditAccess(accessScope);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var record = await RequireRecordAsync(db, id, cancellationToken);
        record.RecordMachineDraft(transcript, providerName, modelName, confidence);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveReviewAsync(
        Guid id,
        string reviewedTranscript,
        HandwritingTranscriptionAccessScope accessScope,
        CancellationToken cancellationToken = default)
    {
        RequireEditAccess(accessScope);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var record = await RequireRecordAsync(db, id, cancellationToken);
        record.SaveReviewedTranscript(reviewedTranscript);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ApproveAsync(
        Guid id,
        HandwritingTranscriptionAccessScope accessScope,
        CancellationToken cancellationToken = default)
    {
        RequireEditAccess(accessScope);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var record = await RequireRecordAsync(db, id, cancellationToken);
        record.Approve();
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkFailedAsync(
        Guid id,
        string message,
        HandwritingTranscriptionAccessScope accessScope,
        CancellationToken cancellationToken = default)
    {
        RequireEditAccess(accessScope);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var record = await RequireRecordAsync(db, id, cancellationToken);
        record.MarkFailed(message);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<HandwritingTranscription> ApplyAccessFilter(
        IQueryable<HandwritingTranscription> query,
        HandwritingTranscriptionAccessScope accessScope)
    {
        return accessScope switch
        {
            HandwritingTranscriptionAccessScope.OwnerAdmin => query,
            HandwritingTranscriptionAccessScope.FamilyArchive => query.Where(record =>
                record.Visibility == HandwritingTranscriptionVisibility.FamilyArchive),
            _ => query.Where(_ => false)
        };
    }

    private static void RequireEditAccess(HandwritingTranscriptionAccessScope accessScope)
    {
        if (!HandwritingTranscriptionPrivacyPolicy.CanEdit(accessScope))
            throw new UnauthorizedAccessException("Owner/admin access is required to modify transcription records.");
    }

    private static async Task<HandwritingTranscription> RequireRecordAsync(
        FamilyTreeDbContext db,
        Guid id,
        CancellationToken cancellationToken)
    {
        return await db.HandwritingTranscriptions.SingleOrDefaultAsync(record => record.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("The transcription record was not found.");
    }

    private static string PersonDisplayName(Person? person)
    {
        if (person is null)
            return "Unlinked document";

        return string.Join(" ", new[] { person.GivenName, person.MiddleName, person.Surname, person.Suffix }
            .Where(part => !string.IsNullOrWhiteSpace(part)));
    }

    private static string? LegacyNumber(Person? person) =>
        person?.Identifiers
            .Where(identifier => identifier.IdentifierType == PersonIdentifierType.LegacyNumber)
            .Select(identifier => identifier.Value)
            .FirstOrDefault();
}
