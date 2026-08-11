using System.Text;
using System.Text.Json;
using LoperFamilyTreeBuilder.Core.Entities;
using LoperFamilyTreeBuilder.Core.Models;
using LoperFamilyTreeBuilder.ImportExport.Gedcom;
using LoperFamilyTreeBuilder.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace LoperFamilyTreeBuilder.Data.Services;

public sealed class GedcomImportService(
    IDbContextFactory<FamilyTreeDbContext> contextFactory,
    ArchiveMediaStorageService storageService,
    DatabaseBackupService backupService)
{
    private readonly GedcomParser _parser = new();
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    public GedcomValidationReport Validate(string fileName, byte[] content)
    {
        var doc = _parser.Parse(content);
        return new GedcomValidationReport(
            doc.Errors.Count == 0, fileName, doc.LineCount, doc.Individuals.Count, doc.Families.Count, doc.Sources.Count,
            doc.Errors, doc.Warnings,
            doc.UnsupportedTags.Select(x => $"Line {x.LineNumber} {x.RecordPointer}: {x.Tag} {x.Value}".Trim()).ToList());
    }

    public async Task<GedcomDryRunReport> AnalyzeAsync(GedcomPreviewRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);
        var doc = _parser.Parse(request.Content);
        if (doc.Errors.Count > 0)
        {
            var errors = doc.Errors.Select(x => new GedcomImportIssueListItem(
                GedcomImportIssueType.ValidationError, string.Empty, x, string.Empty)).ToList();
            var emptyQuality = BuildQuality(doc, [], 0);
            return new GedcomDryRunReport(request.OriginalFileName, false, doc.LineCount, doc.Individuals.Count,
                doc.Families.Count, doc.Sources.Count, 0, doc.Errors.Count, 0, doc.UnsupportedTags.Count,
                emptyQuality, errors, []);
        }

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var build = await BuildPlanAsync(db, doc, cancellationToken);
        return new GedcomDryRunReport(
            request.OriginalFileName, true, doc.LineCount, doc.Individuals.Count, doc.Families.Count, doc.Sources.Count,
            build.Issues.Count(x => x.IssueType == GedcomImportIssueType.DuplicateCandidate),
            build.Issues.Count(x => x.IssueType is GedcomImportIssueType.Conflict or GedcomImportIssueType.LegacyNumberConflict),
            build.Issues.Count(x => x.IssueType == GedcomImportIssueType.LegacyNumberConflict),
            doc.UnsupportedTags.Count,
            build.Quality,
            build.Issues.Take(250).ToList(),
            build.PlannedPeople.Take(100).ToList());
    }

    public async Task<Guid> CreatePreviewAsync(GedcomPreviewRequest request, string actor, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);
        var doc = _parser.Parse(request.Content);
        if (doc.Errors.Count > 0)
            throw new InvalidOperationException("The GEDCOM file contains validation errors. Use GEDCOM Validation to review them first.");

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var build = await BuildPlanAsync(db, doc, cancellationToken);

        await using var contentStream = new MemoryStream(request.Content, writable: false);
        var stored = await storageService.StoreOriginalAsync(contentStream, request.OriginalFileName, "GedcomImports", cancellationToken);

        var plan = new GedcomStoredPlan(build.PlannedPeople, build.PlannedFamilies, doc.Individuals, doc.Families, doc.Sources, build.Quality);
        var session = new GedcomImportSession(request.OriginalFileName, stored.StoredRelativePath, stored.Sha256,
            doc.Individuals.Count, doc.Families.Count, doc.Sources.Count, JsonSerializer.Serialize(plan, JsonOptions));
        session.SetIssueCounts(
            doc.UnsupportedTags.Count,
            build.Issues.Count(x => x.IssueType == GedcomImportIssueType.DuplicateCandidate),
            build.Issues.Count(x => x.IssueType is GedcomImportIssueType.Conflict or GedcomImportIssueType.LegacyNumberConflict));

        db.GedcomImportSessions.Add(session);
        foreach (var issue in build.Issues)
            db.GedcomImportIssues.Add(new GedcomImportIssue(session.Id, issue.IssueType, issue.RecordPointer, issue.Message, issue.Details));

        db.AuditEvents.Add(new AuditEvent("GEDCOM Preview", nameof(GedcomImportSession), session.Id.ToString(), actor,
            $"Created staged GEDCOM preview for {request.OriginalFileName}. Dry-run planning detected {session.DuplicateCandidateCount} duplicate candidates and {session.ConflictCount} conflicts.",
            source: Marker(session.Id)));
        await db.SaveChangesAsync(cancellationToken);
        return session.Id;
    }

    public async Task<IReadOnlyList<GedcomImportSessionListItem>> GetSessionsAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await db.GedcomImportSessions.AsNoTracking().OrderByDescending(x => x.CreatedUtc)
            .Select(x => new GedcomImportSessionListItem(x.Id, x.OriginalFileName, x.Status, x.IndividualCount, x.FamilyCount,
                x.SourceCount, x.UnsupportedTagCount, x.DuplicateCandidateCount, x.ConflictCount, x.CreatedUtc, x.ApprovedUtc, x.AppliedUtc))
            .Take(100).ToListAsync(cancellationToken);
    }

    public async Task<GedcomImportReviewModel?> GetReviewAsync(Guid sessionId, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var session = await db.GedcomImportSessions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == sessionId, cancellationToken);
        if (session is null) return null;
        var plan = JsonSerializer.Deserialize<GedcomStoredPlan>(session.ImportPlanJson, JsonOptions)
            ?? new GedcomStoredPlan([], [], [], [], [], new GedcomDataQualitySummary(0, 0, 0, 0, 0, 0, 0, 0));
        var issues = await db.GedcomImportIssues.AsNoTracking().Where(x => x.ImportSessionId == sessionId)
            .OrderBy(x => x.IssueType).ThenBy(x => x.RecordPointer)
            .Select(x => new GedcomImportIssueListItem(x.IssueType, x.RecordPointer, x.Message, x.Details))
            .ToListAsync(cancellationToken);
        var item = new GedcomImportSessionListItem(session.Id, session.OriginalFileName, session.Status, session.IndividualCount,
            session.FamilyCount, session.SourceCount, session.UnsupportedTagCount, session.DuplicateCandidateCount, session.ConflictCount,
            session.CreatedUtc, session.ApprovedUtc, session.AppliedUtc);
        return new GedcomImportReviewModel(item, issues, plan.PlannedPeople, plan.PlannedFamilies, plan.Quality);
    }

    public async Task ApproveAsync(Guid sessionId, string actor, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var session = await db.GedcomImportSessions.SingleAsync(x => x.Id == sessionId, cancellationToken);
        var validationErrors = await db.GedcomImportIssues.AnyAsync(x => x.ImportSessionId == sessionId && x.IssueType == GedcomImportIssueType.ValidationError, cancellationToken);
        if (validationErrors) throw new InvalidOperationException("Validation errors must be corrected before this import can be approved.");
        session.Approve();
        db.AuditEvents.Add(new AuditEvent("GEDCOM Approve", nameof(GedcomImportSession), session.Id.ToString(), actor,
            $"Approved safe-record import plan for {session.OriginalFileName}. Duplicate and Legacy Number conflict records remain non-creating review items.", source: Marker(session.Id)));
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<GedcomApplyResult> ApplyAsync(Guid sessionId, string actor, CancellationToken cancellationToken = default)
    {
        var backupPath = await backupService.CreatePreImportBackupAsync(cancellationToken);
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var session = await db.GedcomImportSessions.SingleAsync(x => x.Id == sessionId, cancellationToken);
            if (session.Status != GedcomImportStatus.Approved)
                throw new InvalidOperationException("The import must be approved before it can be applied.");

            var plan = JsonSerializer.Deserialize<GedcomStoredPlan>(session.ImportPlanJson, JsonOptions)
                ?? throw new InvalidOperationException("The stored import plan could not be read.");
            var marker = Marker(session.Id);

            var existingSources = await db.SourceRecords.AsNoTracking().ToListAsync(cancellationToken);
            var sourceByTitle = existingSources.GroupBy(x => x.Title, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.First().Id, StringComparer.OrdinalIgnoreCase);
            var sourcePointerMap = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
            var sourcesCreated = 0;
            foreach (var importedSource in plan.Sources)
            {
                var title = string.IsNullOrWhiteSpace(importedSource.Title) ? $"GEDCOM Source {importedSource.Pointer}" : importedSource.Title.Trim();
                if (!sourceByTitle.TryGetValue(title, out var sourceId))
                {
                    var source = new SourceRecord(title, SourceClassification.OnlineDatabase);
                    source.SetDetails(string.Empty, importedSource.Pointer, string.Empty, null, EvidenceQuality.Unknown,
                        "Imported", BuildSourceNotes(importedSource, session), null);
                    db.SourceRecords.Add(source);
                    sourceId = source.Id;
                    sourceByTitle[title] = sourceId;
                    sourcesCreated++;
                    db.AuditEvents.Add(new AuditEvent("GEDCOM Create Source", nameof(SourceRecord), source.Id.ToString(), actor,
                        $"Created source '{title}' from GEDCOM session {session.Id}.", source: marker));
                }
                sourcePointerMap[importedSource.Pointer] = sourceId;
            }

            var pointerToPerson = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
            var peopleCreated = 0;
            var skipped = 0;
            foreach (var planned in plan.PlannedPeople)
            {
                if (!planned.WillCreate)
                {
                    skipped++;
                    if (planned.MatchedPersonId.HasValue && !planned.HasLegacyNumberConflict)
                        pointerToPerson[planned.Pointer] = planned.MatchedPersonId.Value;
                    continue;
                }

                var source = plan.Individuals.Single(i => i.Pointer.Equals(planned.Pointer, StringComparison.OrdinalIgnoreCase));
                var person = new Person(source.GivenName.Trim(), source.Surname.Trim());
                person.SetBirthDate(source.BirthDate);
                if (source.DeathDate.HasValue) person.SetDeathDate(source.DeathDate);
                person.AddIdentifier(PersonIdentifierType.GedcomExternalId, source.Pointer);
                if (!string.IsNullOrEmpty(source.LegacyNumber)) person.AddLegacyNumber(source.LegacyNumber);
                db.People.Add(person);
                pointerToPerson[source.Pointer] = person.Id;
                peopleCreated++;
                db.AuditEvents.Add(new AuditEvent("GEDCOM Create Person", nameof(Person), person.Id.ToString(), actor,
                    $"Created {person.DisplayName} from GEDCOM session {session.Id}.", source: marker));
            }

            var existingCitationKeys = (await db.CitationRecords.AsNoTracking()
                .Select(x => new { x.PersonId, x.SourceRecordId }).ToListAsync(cancellationToken))
                .Select(x => (x.PersonId, x.SourceRecordId)).ToHashSet();
            var citationsCreated = 0;
            var noteCount = 0;
            foreach (var imported in plan.Individuals)
            {
                pointerToPerson.TryGetValue(imported.Pointer, out var personId);
                var hasPerson = personId != Guid.Empty;
                foreach (var note in imported.Notes.Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    db.GedcomImportedNotes.Add(new GedcomImportedNote(session.Id, hasPerson ? personId : null, imported.Pointer, note));
                    noteCount++;
                }
                if (!hasPerson) continue;
                foreach (var sourcePointer in imported.SourcePointers.Distinct(StringComparer.OrdinalIgnoreCase))
                {
                    if (!sourcePointerMap.TryGetValue(sourcePointer, out var sourceId)) continue;
                    if (!existingCitationKeys.Add((personId, sourceId))) continue;
                    var citation = new CitationRecord(personId, sourceId, "GEDCOM imported person evidence", CitationPosition.Supporting);
                    citation.SetDetails(null, sourcePointer, $"Imported from {sourcePointer} during GEDCOM session {session.Id}.", "Imported citation; verify against original source image/record.");
                    db.CitationRecords.Add(citation);
                    citationsCreated++;
                    db.AuditEvents.Add(new AuditEvent("GEDCOM Create Citation", nameof(CitationRecord), citation.Id.ToString(), actor,
                        $"Linked GEDCOM source {sourcePointer} to imported/matched person {personId}.", source: marker));
                }
            }

            var plannedFamilyByPointer = plan.PlannedFamilies.ToDictionary(x => x.Pointer, StringComparer.OrdinalIgnoreCase);
            var parentChildCreated = 0;
            var coupleCreated = 0;
            foreach (var family in plan.Families)
            {
                if (!plannedFamilyByPointer.TryGetValue(family.Pointer, out var familyPlan) || !familyPlan.CanApply) continue;
                if (family.HusbandPointer is not null && family.WifePointer is not null &&
                    pointerToPerson.TryGetValue(family.HusbandPointer, out var husbandId) && pointerToPerson.TryGetValue(family.WifePointer, out var wifeId) && husbandId != wifeId)
                {
                    var exists = await db.CoupleRelationships.AnyAsync(x =>
                        (x.PersonAId == husbandId && x.PersonBId == wifeId) || (x.PersonAId == wifeId && x.PersonBId == husbandId), cancellationToken);
                    if (!exists)
                    {
                        var relationship = new CoupleRelationship(husbandId, wifeId, CoupleRelationshipType.Spouse);
                        relationship.Update(CoupleRelationshipType.Spouse, family.MarriageDate, null,
                            string.IsNullOrWhiteSpace(family.MarriagePlace) ? "Imported from GEDCOM" : $"Imported from GEDCOM; place: {family.MarriagePlace}");
                        db.CoupleRelationships.Add(relationship); coupleCreated++;
                        db.AuditEvents.Add(new AuditEvent("GEDCOM Create Couple Relationship", nameof(CoupleRelationship), relationship.Id.ToString(), actor,
                            $"Created spouse relationship from GEDCOM session {session.Id}.", source: marker));
                    }
                }

                var parentPointers = new[] { family.HusbandPointer, family.WifePointer }.Where(x => !string.IsNullOrWhiteSpace(x)).Cast<string>().ToList();
                foreach (var childPointer in family.ChildPointers)
                {
                    if (!pointerToPerson.TryGetValue(childPointer, out var childId)) continue;
                    foreach (var parentPointer in parentPointers)
                    {
                        if (!pointerToPerson.TryGetValue(parentPointer, out var parentId) || parentId == childId) continue;
                        var exists = await db.ParentChildRelationships.AnyAsync(x => x.ParentPersonId == parentId && x.ChildPersonId == childId, cancellationToken);
                        if (exists) continue;
                        var relationship = new ParentChildRelationship(parentId, childId, ParentChildRelationshipType.Biological);
                        db.ParentChildRelationships.Add(relationship); parentChildCreated++;
                        db.AuditEvents.Add(new AuditEvent("GEDCOM Create Parent Child Relationship", nameof(ParentChildRelationship), relationship.Id.ToString(), actor,
                            $"Created parent-child relationship from GEDCOM session {session.Id}.", source: marker));
                    }
                }
            }

            session.MarkApplied(backupPath);
            db.AuditEvents.Add(new AuditEvent("GEDCOM Apply", nameof(GedcomImportSession), session.Id.ToString(), actor,
                $"Applied GEDCOM session. Created {peopleCreated} people, {parentChildCreated} parent-child relationships, {coupleCreated} couple relationships, {sourcesCreated} sources, {citationsCreated} citations and preserved {noteCount} notes. Backup: {backupPath}", source: marker));
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new GedcomApplyResult(peopleCreated, parentChildCreated, coupleCreated, sourcesCreated, citationsCreated, noteCount, skipped, backupPath);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<GedcomRollbackResult> RollbackAsync(Guid sessionId, string actor, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var session = await db.GedcomImportSessions.SingleAsync(x => x.Id == sessionId, cancellationToken);
            if (session.Status != GedcomImportStatus.Applied || !session.AppliedUtc.HasValue)
                throw new InvalidOperationException("Only an applied GEDCOM session can be rolled back.");
            var marker = Marker(sessionId);
            var createdEvents = await db.AuditEvents.AsNoTracking().Where(x => x.Source == marker && x.Action.StartsWith("GEDCOM Create"))
                .ToListAsync(cancellationToken);

            static Guid[] Ids(IEnumerable<AuditEvent> events, string type) => events.Where(x => x.EntityType == type)
                .Select(x => Guid.TryParse(x.EntityId, out var id) ? id : Guid.Empty).Where(x => x != Guid.Empty).Distinct().ToArray();

            var personIds = Ids(createdEvents, nameof(Person));
            var parentIds = Ids(createdEvents, nameof(ParentChildRelationship));
            var coupleIds = Ids(createdEvents, nameof(CoupleRelationship));
            var citationIds = Ids(createdEvents, nameof(CitationRecord));
            var sourceIds = Ids(createdEvents, nameof(SourceRecord));
            var personIdStrings = personIds.Select(x => x.ToString()).ToArray();
            var createdEntityIds = createdEvents.Select(x => x.EntityId).Distinct().ToArray();

            if (createdEntityIds.Length > 0)
            {
                var edited = await db.AuditEvents.AsNoTracking().AnyAsync(x => x.OccurredUtc > session.AppliedUtc.Value &&
                    createdEntityIds.Contains(x.EntityId) && x.Source != marker, cancellationToken);
                if (edited) throw new InvalidOperationException("Rollback is blocked because at least one imported record was edited after the import. Use the pre-import backup for a controlled restore instead.");
            }

            if (personIdStrings.Length > 0)
            {
                var foreignParentLinks = await db.ParentChildRelationships.AsNoTracking().AnyAsync(x =>
                    (personIds.Contains(x.ParentPersonId) || personIds.Contains(x.ChildPersonId)) && !parentIds.Contains(x.Id), cancellationToken);
                var foreignCoupleLinks = await db.CoupleRelationships.AsNoTracking().AnyAsync(x =>
                    (personIds.Contains(x.PersonAId) || personIds.Contains(x.PersonBId)) && !coupleIds.Contains(x.Id), cancellationToken);
                if (foreignParentLinks || foreignCoupleLinks)
                    throw new InvalidOperationException("Rollback is blocked because imported people now have relationships created outside this import session.");
            }

            var citations = await db.CitationRecords.Where(x => citationIds.Contains(x.Id)).ToListAsync(cancellationToken);
            var notes = await db.GedcomImportedNotes.Where(x => x.ImportSessionId == sessionId).ToListAsync(cancellationToken);
            var parentRelationships = await db.ParentChildRelationships.Where(x => parentIds.Contains(x.Id)).ToListAsync(cancellationToken);
            var coupleRelationships = await db.CoupleRelationships.Where(x => coupleIds.Contains(x.Id)).ToListAsync(cancellationToken);
            db.CitationRecords.RemoveRange(citations); db.GedcomImportedNotes.RemoveRange(notes);
            db.ParentChildRelationships.RemoveRange(parentRelationships); db.CoupleRelationships.RemoveRange(coupleRelationships);
            await db.SaveChangesAsync(cancellationToken);

            var people = await db.People.Where(x => personIds.Contains(x.Id)).ToListAsync(cancellationToken);
            db.People.RemoveRange(people);
            await db.SaveChangesAsync(cancellationToken);

            var sourcesRemoved = 0;
            foreach (var sourceId in sourceIds)
            {
                var stillUsed = await db.CitationRecords.AsNoTracking().AnyAsync(x => x.SourceRecordId == sourceId, cancellationToken);
                if (stillUsed) continue;
                var source = await db.SourceRecords.SingleOrDefaultAsync(x => x.Id == sourceId, cancellationToken);
                if (source is not null) { db.SourceRecords.Remove(source); sourcesRemoved++; }
            }

            session.MarkRolledBack($"Rolled back imported records. Pre-import backup retained at {session.BackupFilePath}.");
            db.AuditEvents.Add(new AuditEvent("GEDCOM Rollback", nameof(GedcomImportSession), session.Id.ToString(), actor,
                $"Rolled back records created by GEDCOM session {session.Id}. Pre-import backup remains available at {session.BackupFilePath}.", source: marker));
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new GedcomRollbackResult(people.Count, parentRelationships.Count, coupleRelationships.Count, citations.Count, notes.Count, sourcesRemoved);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private async Task<PlanBuildResult> BuildPlanAsync(FamilyTreeDbContext db, GedcomDocument doc, CancellationToken cancellationToken)
    {
        var people = await db.People.AsNoTracking().Select(x => new ExistingPerson(x.Id, x.GivenName, x.Surname, x.BirthDate)).ToListAsync(cancellationToken);
        var identifiers = await db.PersonIdentifiers.AsNoTracking()
            .Where(x => x.IdentifierType == PersonIdentifierType.GedcomExternalId || x.IdentifierType == PersonIdentifierType.LegacyNumber)
            .Select(x => new ExistingIdentifier(x.PersonId, x.IdentifierType, x.Value)).ToListAsync(cancellationToken);
        var gedcomByValue = identifiers.Where(x => x.Type == PersonIdentifierType.GedcomExternalId)
            .GroupBy(x => x.Value, StringComparer.OrdinalIgnoreCase).ToDictionary(x => x.Key, x => x.Select(y => y.PersonId).Distinct().ToList(), StringComparer.OrdinalIgnoreCase);
        var legacyByValue = identifiers.Where(x => x.Type == PersonIdentifierType.LegacyNumber)
            .GroupBy(x => x.Value, StringComparer.Ordinal).ToDictionary(x => x.Key, x => x.Select(y => y.PersonId).Distinct().ToList(), StringComparer.Ordinal);
        var legacyByPerson = identifiers.Where(x => x.Type == PersonIdentifierType.LegacyNumber)
            .GroupBy(x => x.PersonId).ToDictionary(x => x.Key, x => x.Select(y => y.Value).ToList());
        var peopleByName = people.GroupBy(x => NameKey(x.GivenName, x.Surname), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.ToList(), StringComparer.OrdinalIgnoreCase);

        var issues = new List<GedcomImportIssueListItem>();
        var plannedPeople = new List<GedcomPlannedPerson>(doc.Individuals.Count);
        foreach (var individual in doc.Individuals)
        {
            Guid? externalMatch = null;
            if (gedcomByValue.TryGetValue(individual.Pointer, out var externalMatches) && externalMatches.Count == 1) externalMatch = externalMatches[0];
            Guid? legacyMatch = null;
            if (!string.IsNullOrEmpty(individual.LegacyNumber) && legacyByValue.TryGetValue(individual.LegacyNumber, out var legacyMatches) && legacyMatches.Count == 1) legacyMatch = legacyMatches[0];

            Guid? nameMatch = null;
            var multipleNameMatches = false;
            if (peopleByName.TryGetValue(NameKey(individual.GivenName, individual.Surname), out var nameCandidates))
            {
                var candidates = individual.BirthDate.HasValue ? nameCandidates.Where(x => x.BirthDate == individual.BirthDate).ToList() : nameCandidates;
                if (candidates.Count == 1) nameMatch = candidates[0].Id;
                else if (candidates.Count > 1) multipleNameMatches = true;
            }

            var distinctMatches = new[] { externalMatch, legacyMatch, nameMatch }.Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList();
            var matchedId = distinctMatches.Count == 1 ? distinctMatches[0] : (Guid?)null;
            var hasLegacyConflict = false;
            var reasons = new List<string>();

            if (distinctMatches.Count > 1)
            {
                hasLegacyConflict = legacyMatch.HasValue;
                reasons.Add("GEDCOM ID, Legacy Number, or name/date evidence points to different existing people.");
                issues.Add(new GedcomImportIssueListItem(hasLegacyConflict ? GedcomImportIssueType.LegacyNumberConflict : GedcomImportIssueType.Conflict,
                    individual.Pointer, reasons[^1], $"Incoming Legacy Number: {individual.LegacyNumber}"));
            }
            if (multipleNameMatches)
            {
                reasons.Add("Multiple existing people match this imported name/date. Manual review is required.");
                issues.Add(new GedcomImportIssueListItem(GedcomImportIssueType.Conflict, individual.Pointer, reasons[^1], Display(individual)));
            }

            if (matchedId.HasValue)
            {
                if (externalMatch == matchedId) reasons.Add("GEDCOM external ID already exists.");
                else if (legacyMatch == matchedId) reasons.Add("Exact Legacy Number already exists on an existing person.");
                else reasons.Add(individual.BirthDate.HasValue ? "Same given name, surname, and exact birth date." : "Same given name and surname; birth date is not exact in the import.");

                if (!string.IsNullOrEmpty(individual.LegacyNumber) && legacyByPerson.TryGetValue(matchedId.Value, out var existingLegacyValues) &&
                    existingLegacyValues.Count > 0 && !existingLegacyValues.Contains(individual.LegacyNumber, StringComparer.Ordinal))
                {
                    hasLegacyConflict = true;
                    var existing = string.Join(", ", existingLegacyValues);
                    var message = "Incoming Legacy Number conflicts with the protected Legacy Number already stored for the matched person.";
                    issues.Add(new GedcomImportIssueListItem(GedcomImportIssueType.LegacyNumberConflict, individual.Pointer, message,
                        $"Existing: {existing}; incoming: {individual.LegacyNumber}. Existing value will not be changed."));
                    reasons.Add(message);
                }
            }

            if (!string.IsNullOrEmpty(individual.LegacyNumber) && legacyByValue.TryGetValue(individual.LegacyNumber, out var exactLegacyOwners) && exactLegacyOwners.Count > 1)
            {
                hasLegacyConflict = true;
                var message = "The incoming Legacy Number is already assigned to multiple existing people. Manual data repair is required.";
                issues.Add(new GedcomImportIssueListItem(GedcomImportIssueType.LegacyNumberConflict, individual.Pointer, message, individual.LegacyNumber));
                reasons.Add(message);
            }

            var isDuplicate = matchedId.HasValue;
            if (isDuplicate)
                issues.Add(new GedcomImportIssueListItem(GedcomImportIssueType.DuplicateCandidate, individual.Pointer,
                    string.Join(" ", reasons.Distinct()), $"Imported person: {Display(individual)}"));

            var willCreate = !isDuplicate && !multipleNameMatches && distinctMatches.Count <= 1 && !hasLegacyConflict;
            plannedPeople.Add(new GedcomPlannedPerson(individual.Pointer, individual.GivenName, individual.Surname, Display(individual),
                individual.BirthDate, individual.DeathDate, individual.BirthPlace, individual.DeathPlace, individual.LegacyNumber,
                individual.Pointer, individual.Notes.Count, individual.SourcePointers.Count, isDuplicate, matchedId,
                string.Join(" ", reasons.Distinct()), hasLegacyConflict, willCreate));
        }

        var planByPointer = plannedPeople.ToDictionary(x => x.Pointer, StringComparer.OrdinalIgnoreCase);
        var plannedFamilies = new List<GedcomPlannedFamily>(doc.Families.Count);
        foreach (var family in doc.Families)
        {
            var refs = new[] { family.HusbandPointer, family.WifePointer }.Concat(family.ChildPointers.Cast<string?>())
                .Where(x => !string.IsNullOrWhiteSpace(x)).Cast<string>().ToList();
            var missing = refs.Where(x => !planByPointer.ContainsKey(x)).ToList();
            var blocked = refs.Where(x => planByPointer.TryGetValue(x, out var p) && !p.WillCreate && !p.MatchedPersonId.HasValue).ToList();
            var canApply = missing.Count == 0 && blocked.Count == 0;
            var noteParts = new List<string>();
            if (missing.Count > 0) noteParts.Add($"Missing individual reference(s): {string.Join(", ", missing)}");
            if (blocked.Count > 0) noteParts.Add($"Blocked by unresolved person conflict(s): {string.Join(", ", blocked)}");
            var note = string.Join("; ", noteParts);
            if (!canApply) issues.Add(new GedcomImportIssueListItem(GedcomImportIssueType.Conflict, family.Pointer,
                "Family record cannot be applied safely.", note));
            plannedFamilies.Add(new GedcomPlannedFamily(family.Pointer, family.HusbandPointer, family.WifePointer,
                family.ChildPointers, family.MarriageDate, family.MarriagePlace, canApply, note));
        }

        foreach (var tag in doc.UnsupportedTags)
            issues.Add(new GedcomImportIssueListItem(GedcomImportIssueType.UnsupportedTag,
                string.IsNullOrWhiteSpace(tag.RecordPointer) ? $"line:{tag.LineNumber}" : tag.RecordPointer,
                $"Unsupported/custom tag {tag.Tag}", $"Line {tag.LineNumber}: {tag.Value}"));
        foreach (var warning in doc.Warnings)
            issues.Add(new GedcomImportIssueListItem(GedcomImportIssueType.ValidationWarning, string.Empty, warning, string.Empty));

        var legacyConflicts = issues.Count(x => x.IssueType == GedcomImportIssueType.LegacyNumberConflict);
        var quality = BuildQuality(doc, plannedPeople, legacyConflicts);
        return new PlanBuildResult(plannedPeople, plannedFamilies, issues, quality);
    }

    private static GedcomDataQualitySummary BuildQuality(GedcomDocument doc, IReadOnlyList<GedcomPlannedPerson> plannedPeople, int legacyConflicts)
    {
        var places = doc.Individuals.SelectMany(x => new[] { x.BirthPlace, x.DeathPlace })
            .Concat(doc.Families.Select(x => x.MarriagePlace)).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        var variantGroups = places.GroupBy(NormalizePlace, StringComparer.OrdinalIgnoreCase)
            .Count(g => g.Select(x => x).Distinct(StringComparer.Ordinal).Count() > 1);
        return new GedcomDataQualitySummary(
            doc.Individuals.Count(x => !x.BirthDate.HasValue),
            doc.Individuals.Count(x => !x.DeathDate.HasValue),
            doc.Individuals.Count(x => x.FamilyChildPointers.Count == 0),
            doc.Individuals.Count(x => x.SourcePointers.Count == 0),
            doc.Individuals.Count(x => !string.IsNullOrEmpty(x.LegacyNumber)),
            legacyConflicts,
            doc.Individuals.Sum(x => x.Notes.Count) + doc.Families.Sum(x => x.Notes.Count) + doc.Sources.Sum(x => x.Notes.Count),
            variantGroups);
    }

    private static string NormalizePlace(string value)
    {
        var chars = value.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray();
        return new string(chars);
    }

    private static string NameKey(string given, string surname) => $"{given.Trim()}\u001f{surname.Trim()}";
    private static string Display(GedcomIndividual individual) => string.IsNullOrWhiteSpace(individual.Name)
        ? string.Join(" ", new[] { individual.GivenName, individual.Surname }.Where(x => !string.IsNullOrWhiteSpace(x)))
        : individual.Name.Replace("/", string.Empty).Trim();
    private static string Marker(Guid sessionId) => $"GEDCOM:{sessionId:D}";
    private static void ValidateRequest(GedcomPreviewRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.Content.Length == 0) throw new InvalidOperationException("Select a GEDCOM file before analyzing it.");
        if (string.IsNullOrWhiteSpace(request.OriginalFileName)) throw new InvalidOperationException("GEDCOM filename is required.");
    }
    private static string BuildSourceNotes(GedcomSource source, GedcomImportSession session)
    {
        var text = new StringBuilder($"Imported from GEDCOM source {source.Pointer} in session {session.Id}.");
        if (!string.IsNullOrWhiteSpace(source.Author)) text.Append(" Author: ").Append(source.Author).Append('.');
        if (!string.IsNullOrWhiteSpace(source.PublicationFacts)) text.Append(" Publication: ").Append(source.PublicationFacts).Append('.');
        if (source.Notes.Count > 0) text.Append(" Notes: ").Append(string.Join(" | ", source.Notes));
        return text.ToString();
    }

    private sealed record ExistingPerson(Guid Id, string GivenName, string Surname, DateOnly? BirthDate);
    private sealed record ExistingIdentifier(Guid PersonId, PersonIdentifierType Type, string Value);
    private sealed record PlanBuildResult(IReadOnlyList<GedcomPlannedPerson> PlannedPeople,
        IReadOnlyList<GedcomPlannedFamily> PlannedFamilies, IReadOnlyList<GedcomImportIssueListItem> Issues, GedcomDataQualitySummary Quality);

    public sealed record GedcomStoredPlan(IReadOnlyList<GedcomPlannedPerson> PlannedPeople,
        IReadOnlyList<GedcomPlannedFamily> PlannedFamilies, IReadOnlyList<GedcomIndividual> Individuals,
        IReadOnlyList<GedcomFamily> Families, IReadOnlyList<GedcomSource> Sources, GedcomDataQualitySummary Quality);
}
