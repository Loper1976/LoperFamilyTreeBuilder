using System.Data;
using System.Security.Cryptography;
using System.Text.Json;
using LoperFamilyTreeBuilder.Core.Entities;
using LoperFamilyTreeBuilder.Core.Policies;
using LoperFamilyTreeBuilder.ImportExport.Gedcom;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using ResearchAgent.Core.Integration;

namespace LoperFamilyTreeBuilder.Data.Services;

public sealed record ReviewedGedcomImportRequest(
    string ExpectedSha256,
    int ExpectedNewPersonCount,
    string Reviewer,
    string Confirmation,
    bool IncludeProtectedUncertain = false);

public sealed record ReviewedGedcomImportResult(
    Guid ImportId,
    string GedcomSha256,
    string BackupReference,
    int PeopleImported,
    int ExistingPeopleMatched,
    int RelationshipsImported,
    int RelationshipsSkipped);

/// <summary>
/// Imports only preview-classified new people. Exact duplicates are mapped to the
/// existing person, possible duplicates are deliberately left unresolved, and a
/// verified backup must finish before the accepted tree transaction begins.
/// </summary>
public sealed class GedcomReviewedImportService(
    IDbContextFactory<FamilyTreeDbContext> contextFactory,
    GedcomParser parser,
    GedcomImportPreviewService previewService,
    IHostBackupGate backupGate)
{
    public async Task<ReviewedGedcomImportResult> ImportAsync(
        ReadOnlyMemory<byte> gedcomBytes,
        ReviewedGedcomImportRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (gedcomBytes.IsEmpty) throw new InvalidOperationException("The GEDCOM file is empty.");
        if (string.IsNullOrWhiteSpace(request.Reviewer)) throw new InvalidOperationException("A reviewer is required.");

        var sha256 = Convert.ToHexString(SHA256.HashData(gedcomBytes.Span));
        if (!string.Equals(sha256, request.ExpectedSha256?.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The GEDCOM changed after review. Run the preview again.");

        await using var previewStream = new MemoryStream(gedcomBytes.ToArray(), writable: false);
        var preview = await previewService.PreviewAsync(previewStream, cancellationToken);
        if (!preview.CanProceedToReview || preview.VerificationErrorCount > 0)
            throw new InvalidOperationException("Import is blocked by GEDCOM structural or timeline errors.");
        if (preview.NewPersonCount != request.ExpectedNewPersonCount)
            throw new InvalidOperationException("The accepted tree changed after review. Run the preview again.");
        if (!string.Equals(request.Confirmation?.Trim(), $"IMPORT {preview.NewPersonCount} PEOPLE", StringComparison.Ordinal))
            throw new InvalidOperationException($"Confirmation must be exactly: IMPORT {preview.NewPersonCount} PEOPLE");

        await using var parseStream = new MemoryStream(gedcomBytes.ToArray(), writable: false);
        var document = await parser.ParseAsync(parseStream, cancellationToken: cancellationToken);
        var selectedCandidates = preview.Candidates
            .Where(x => x.Disposition == GedcomImportDisposition.NewPerson)
            .Where(x => request.IncludeProtectedUncertain || x.Privacy != GedcomPrivacyClassification.ProtectedUncertain)
            .ToDictionary(x => x.ExternalId, StringComparer.Ordinal);

        var backupReference = await backupGate.CreatePreChangeBackupAsync(
            $"Reviewed GEDCOM import {sha256}", cancellationToken);
        await backupGate.VerifyBackupAsync(backupReference, cancellationToken);

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var importId = Guid.NewGuid();
        try
        {
            var existingGedcomIds = await db.PersonIdentifiers.AsNoTracking()
                .Where(x => x.IdentifierType == PersonIdentifierType.GedcomExternalId)
                .Select(x => x.Value)
                .ToArrayAsync(cancellationToken);
            if (selectedCandidates.Keys.Intersect(existingGedcomIds, StringComparer.Ordinal).Any())
                throw new InvalidOperationException("A selected GEDCOM person was already imported. Run the preview again.");

            var personMap = preview.Candidates
                .Where(x => x.Disposition == GedcomImportDisposition.ExactDuplicate && x.MatchingPersonId.HasValue)
                .ToDictionary(x => x.ExternalId, x => x.MatchingPersonId!.Value, StringComparer.Ordinal);
            var exactMatchCount = personMap.Count;
            var importedCount = 0;

            foreach (var source in document.Individuals.Where(x => selectedCandidates.ContainsKey(x.ExternalId)))
            {
                var person = new Person(source.GivenName, source.Surname);
                person.UpdateName(source.GivenName, string.Empty, source.Surname, string.Empty);
                person.SetBirthDate(source.BirthDate?.ExactDate);
                person.SetDeathDate(source.DeathDate?.ExactDate);
                if (source.HasDeathRecord)
                    person.SetLivingStatus(false);
                else
                    person.SetLivingStatus(source.Privacy == GedcomPrivacyClassification.ProtectedUncertain);

                var sequence = await GetNextLoperIdSequenceAsync(db, cancellationToken);
                var loperId = LoperIdPolicy.Format(sequence);
                person.AddLoperId(loperId);
                person.AddIdentifier(PersonIdentifierType.GedcomExternalId, source.ExternalId, isProtected: true);
                db.People.Add(person);
                personMap[source.ExternalId] = person.Id;
                importedCount++;

                db.AuditEvents.Add(new AuditEvent(
                    "GEDCOM Import", nameof(Person), person.Id.ToString(), request.Reviewer,
                    "Imported a reviewed GEDCOM person with a new protected LOPER ID.",
                    newValueJson: JsonSerializer.Serialize(new
                    {
                        ImportId = importId, GedcomSha256 = sha256, source.ExternalId,
                        LoperId = loperId, source.Privacy, BirthText = source.BirthDate?.OriginalText,
                        DeathText = source.DeathDate?.OriginalText
                    })));
            }

            var relationshipsImported = 0;
            var relationshipsSkipped = 0;
            foreach (var family in document.Families)
            {
                foreach (var childExternalId in family.ChildIds)
                {
                    foreach (var parentExternalId in new[] { family.HusbandId, family.WifeId }.Where(x => x is not null))
                    {
                        if (!personMap.TryGetValue(parentExternalId!, out var parentId) ||
                            !personMap.TryGetValue(childExternalId, out var childId))
                        {
                            relationshipsSkipped++;
                            continue;
                        }

                        var relationship = new ParentChildRelationship(
                            parentId, childId, ParentChildRelationshipType.Biological);
                        db.ParentChildRelationships.Add(relationship);
                        relationshipsImported++;
                    }
                }
            }

            db.AuditEvents.Add(new AuditEvent(
                "GEDCOM Import Completed", "GedcomImport", importId.ToString(), request.Reviewer,
                $"Imported {importedCount} reviewed people and {relationshipsImported} parent-child relationships after verified backup.",
                newValueJson: JsonSerializer.Serialize(new
                {
                    ImportId = importId, GedcomSha256 = sha256, BackupReference = backupReference,
                    PeopleImported = importedCount, ExistingPeopleMatched = exactMatchCount,
                    RelationshipsImported = relationshipsImported, RelationshipsSkipped = relationshipsSkipped,
                    request.IncludeProtectedUncertain
                })));

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new ReviewedGedcomImportResult(importId, sha256, backupReference,
                importedCount, exactMatchCount, relationshipsImported, relationshipsSkipped);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private static async Task<long> GetNextLoperIdSequenceAsync(
        FamilyTreeDbContext db, CancellationToken cancellationToken)
    {
        var connection = db.Database.GetDbConnection();
        if (connection.State != ConnectionState.Open)
            await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
        command.CommandText = "SELECT NEXT VALUE FOR [LoperIdSequence]";
        return Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
    }
}
