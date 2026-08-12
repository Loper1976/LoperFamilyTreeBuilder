using System.Diagnostics;
using System.Text.RegularExpressions;
using LoperFamilyTreeBuilder.Core.Entities;
using LoperFamilyTreeBuilder.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace LoperFamilyTreeBuilder.Data.Services;

public sealed class TreeIntegrityService(IDbContextFactory<FamilyTreeDbContext> contextFactory)
{
    public const string RulesVersion = "1.2.3";

    public async Task<TreeIntegrityScanResult> RunScanAsync(string actor, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var run = new TreeIntegrityScanRun(actor, RulesVersion);
        db.TreeIntegrityScanRuns.Add(run);
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            var people = await db.People.AsNoTracking().Include(x => x.Identifiers).ToListAsync(cancellationToken);
            var parentLinks = await db.ParentChildRelationships.AsNoTracking().ToListAsync(cancellationToken);
            var coupleLinks = await db.CoupleRelationships.AsNoTracking().ToListAsync(cancellationToken);
            var timeline = await db.TimelineEventRecords.AsNoTracking().ToListAsync(cancellationToken);
            var burials = await db.BurialRecords.AsNoTracking().ToListAsync(cancellationToken);
            var military = await db.MilitaryServiceRecords.AsNoTracking().ToListAsync(cancellationToken);
            var citations = await db.CitationRecords.AsNoTracking().ToListAsync(cancellationToken);

            var findings = BuildFindings(people, parentLinks, coupleLinks, timeline, burials, military, citations);
            await SynchronizeIssuesAsync(db, findings, cancellationToken);

            stopwatch.Stop();
            var critical = findings.Count(x => x.Severity == TreeIntegritySeverity.Critical);
            var high = findings.Count(x => x.Severity == TreeIntegritySeverity.High);
            var medium = findings.Count(x => x.Severity == TreeIntegritySeverity.Medium);
            var low = findings.Count(x => x.Severity == TreeIntegritySeverity.Low);
            var informational = findings.Count(x => x.Severity == TreeIntegritySeverity.Informational);
            run.Complete(critical, high, medium, low, informational, stopwatch.ElapsedMilliseconds);

            db.AuditEvents.Add(new AuditEvent(
                "Run automated tree error checker",
                nameof(TreeIntegrityScanRun),
                run.Id.ToString(),
                actor,
                $"Tree integrity scan completed with {findings.Count} active finding(s). No genealogy data or Legacy Numbers were changed.",
                source: "Tree Integrity"));

            await db.SaveChangesAsync(cancellationToken);
            return new TreeIntegrityScanResult(run.Id, findings.Count, critical, high, medium, low, informational, stopwatch.ElapsedMilliseconds, RulesVersion);
        }
        catch
        {
            stopwatch.Stop();
            run.Fail(stopwatch.ElapsedMilliseconds);
            await db.SaveChangesAsync(cancellationToken);
            throw;
        }
    }

    public async Task<TreeIntegrityDashboard> GetDashboardAsync(TreeIntegrityFilterRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.Page = Math.Max(1, request.Page);
        request.PageSize = Math.Clamp(request.PageSize, 10, 200);
        var search = (request.Search ?? string.Empty).Trim();

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var query = db.TreeIntegrityIssues.AsNoTracking().AsQueryable();
        if (request.ActiveOnly)
            query = query.Where(x => x.IsActive);
        if (request.Severity.HasValue)
            query = query.Where(x => x.Severity == request.Severity.Value);
        if (request.Status.HasValue)
            query = query.Where(x => x.Status == request.Status.Value);
        if (request.IssueType.HasValue)
            query = query.Where(x => x.IssueType == request.IssueType.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.Title.Contains(search) || x.Description.Contains(search) || x.EvidenceSummary.Contains(search));
        }

        var total = await query.CountAsync(cancellationToken);
        var raw = await query
            .OrderBy(x => x.Status == TreeIntegrityIssueStatus.Open ? 0 : 1)
            .ThenBy(x => x.Severity)
            .ThenByDescending(x => x.LastDetectedUtc)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var personIds = raw.SelectMany(x => new[] { x.PersonId, x.RelatedPersonId }).Where(x => x.HasValue).Select(x => x!.Value).Distinct().ToList();
        var people = await db.People.AsNoTracking().Include(x => x.Identifiers).Where(x => personIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, cancellationToken);
        var items = raw.Select(x => ToListItem(x, people)).ToList();

        var active = db.TreeIntegrityIssues.AsNoTracking().Where(x => x.IsActive);
        var activeTotal = await active.CountAsync(cancellationToken);
        var critical = await active.CountAsync(x => x.Severity == TreeIntegritySeverity.Critical, cancellationToken);
        var high = await active.CountAsync(x => x.Severity == TreeIntegritySeverity.High, cancellationToken);
        var medium = await active.CountAsync(x => x.Severity == TreeIntegritySeverity.Medium, cancellationToken);
        var low = await active.CountAsync(x => x.Severity == TreeIntegritySeverity.Low, cancellationToken);
        var informational = await active.CountAsync(x => x.Severity == TreeIntegritySeverity.Informational, cancellationToken);
        var dismissed = await db.TreeIntegrityIssues.AsNoTracking().CountAsync(x => x.Status == TreeIntegrityIssueStatus.Dismissed, cancellationToken);
        var resolved = await db.TreeIntegrityIssues.AsNoTracking().CountAsync(x => x.Status == TreeIntegrityIssueStatus.Resolved, cancellationToken);
        var lastRun = await db.TreeIntegrityScanRuns.AsNoTracking().Where(x => x.Status == "Completed").OrderByDescending(x => x.CompletedUtc).FirstOrDefaultAsync(cancellationToken);
        var scans = await db.TreeIntegrityScanRuns.AsNoTracking().OrderByDescending(x => x.StartedUtc).Take(10).Select(x => new TreeIntegrityScanRunSummary(x.Id, x.StartedBy, x.RulesVersion, x.Status, x.TotalFindings, x.CriticalCount, x.HighCount, x.MediumCount, x.LowCount, x.InformationalCount, x.DurationMilliseconds, x.StartedUtc, x.CompletedUtc)).ToListAsync(cancellationToken);

        var summary = new TreeIntegritySummary(activeTotal, critical, high, medium, low, informational, dismissed, resolved, lastRun?.CompletedUtc, lastRun?.DurationMilliseconds, RulesVersion);
        return new TreeIntegrityDashboard(summary, new PagedResult<TreeIntegrityIssueListItem>(items, request.Page, request.PageSize, total), scans);
    }

    public async Task DismissIssueAsync(Guid issueId, string reason, string actor, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var issue = await db.TreeIntegrityIssues.SingleOrDefaultAsync(x => x.Id == issueId, cancellationToken) ?? throw new InvalidOperationException("Integrity issue not found.");
        issue.Dismiss(reason, actor);
        db.AuditEvents.Add(new AuditEvent("Dismiss tree integrity issue", nameof(TreeIntegrityIssue), issue.Id.ToString(), actor, $"Issue {issue.IssueType} dismissed with a preserved review reason. No genealogy data changed.", source: "Tree Integrity"));
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ResolveIssueAsync(Guid issueId, string reason, string actor, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var issue = await db.TreeIntegrityIssues.SingleOrDefaultAsync(x => x.Id == issueId, cancellationToken) ?? throw new InvalidOperationException("Integrity issue not found.");
        issue.Resolve(reason, actor);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task ReopenIssueAsync(Guid issueId, string actor, CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var issue = await db.TreeIntegrityIssues.SingleOrDefaultAsync(x => x.Id == issueId, cancellationToken) ?? throw new InvalidOperationException("Integrity issue not found.");
        issue.Reopen(actor);
        await db.SaveChangesAsync(cancellationToken);
    }

    internal static IReadOnlyList<TreeIntegrityFinding> BuildFindings(
        IReadOnlyList<Person> people,
        IReadOnlyList<ParentChildRelationship> parentLinks,
        IReadOnlyList<CoupleRelationship> coupleLinks,
        IReadOnlyList<TimelineEventRecord> timeline,
        IReadOnlyList<BurialRecord> burials,
        IReadOnlyList<MilitaryServiceRecord> military,
        IReadOnlyList<CitationRecord> citations)
    {
        var findings = new Dictionary<string, TreeIntegrityFinding>(StringComparer.Ordinal);
        var byId = people.ToDictionary(x => x.Id);
        void Add(TreeIntegrityFinding finding) => findings.TryAdd(finding.IssueKey, finding);

        foreach (var person in people)
        {
            var legacy = Legacy(person);
            if (person.BirthDate.HasValue && person.DeathDate.HasValue && person.DeathDate.Value < person.BirthDate.Value)
                Add(Finding($"DeathBeforeBirth:{person.Id}", TreeIntegrityIssueType.DeathBeforeBirth, TreeIntegritySeverity.Critical, "Death date occurs before birth date", $"{person.DisplayName} has a death date earlier than the recorded birth date.", $"Birth {person.BirthDate:yyyy-MM-dd}; death {person.DeathDate:yyyy-MM-dd}; Legacy # {legacy}", person.Id));

            if (!person.BirthDate.HasValue)
                Add(Finding($"MissingBirth:{person.Id}", TreeIntegrityIssueType.MissingBirthDate, TreeIntegritySeverity.Low, "Birth date is missing", $"{person.DisplayName} has no recorded birth date.", $"Legacy # {legacy}. Research suggestion only; no automatic date is inferred.", person.Id));

            if (!person.IsLiving && !person.DeathDate.HasValue)
                Add(Finding($"MissingDeath:{person.Id}", TreeIntegrityIssueType.MissingDeathDateForDeceasedPerson, TreeIntegritySeverity.Low, "Deceased person is missing a death date", $"{person.DisplayName} is marked deceased but has no death date.", $"Legacy # {legacy}.", person.Id));

            if (person.BirthDate.HasValue && !HasCitation(citations, person.Id, "birth"))
                Add(Finding($"MissingBirthCitation:{person.Id}", TreeIntegrityIssueType.MissingBirthCitation, TreeIntegritySeverity.Low, "Birth fact has no specific citation", $"{person.DisplayName}'s recorded birth date does not have a citation whose subject identifies the birth fact.", $"Birth {person.BirthDate:yyyy-MM-dd}; Legacy # {legacy}.", person.Id));

            if (person.DeathDate.HasValue && !HasCitation(citations, person.Id, "death"))
                Add(Finding($"MissingDeathCitation:{person.Id}", TreeIntegrityIssueType.MissingDeathCitation, TreeIntegritySeverity.Low, "Death fact has no specific citation", $"{person.DisplayName}'s recorded death date does not have a citation whose subject identifies the death fact.", $"Death {person.DeathDate:yyyy-MM-dd}; Legacy # {legacy}.", person.Id));
        }

        foreach (var duplicateLegacy in people.Select(p => new { Person = p, Legacy = Legacy(p) }).Where(x => !string.IsNullOrEmpty(x.Legacy)).GroupBy(x => x.Legacy, StringComparer.Ordinal).Where(g => g.Select(x => x.Person.Id).Distinct().Count() > 1))
        {
            var group = duplicateLegacy.Select(x => x.Person).OrderBy(x => x.DisplayName).ToList();
            for (var i = 1; i < group.Count; i++)
                Add(Finding($"DuplicateLegacy:{duplicateLegacy.Key}:{group[i].Id}", TreeIntegrityIssueType.DuplicateLegacyNumber, TreeIntegritySeverity.Critical, "Duplicate protected Legacy Number", $"More than one person has the exact protected Legacy Number {duplicateLegacy.Key}. The checker will never change either value automatically.", $"{group[0].DisplayName} and {group[i].DisplayName} both use Legacy # {duplicateLegacy.Key}.", group[i].Id, group[0].Id));
        }

        foreach (var group in people.Where(x => x.BirthDate.HasValue).GroupBy(x => $"{NormalizeName(x.DisplayName)}|{x.BirthDate:yyyy-MM-dd}").Where(g => g.Count() > 1))
        {
            var members = group.OrderBy(x => x.Id).ToList();
            for (var i = 1; i < members.Count; i++)
                Add(Finding($"PossibleDuplicatePerson:{members[0].Id}:{members[i].Id}", TreeIntegrityIssueType.PossibleDuplicatePerson, TreeIntegritySeverity.Medium, "Possible duplicate person", "Two people share the same normalized name and exact birth date. Review before merging; no automatic merge will occur.", $"{members[0].DisplayName} and {members[i].DisplayName}; birth {members[i].BirthDate:yyyy-MM-dd}.", members[i].Id, members[0].Id));
        }

        foreach (var group in parentLinks.GroupBy(x => new { x.ParentPersonId, x.ChildPersonId }).Where(g => g.Count() > 1))
        {
            var first = group.First();
            Add(Finding($"DuplicateParentChild:{first.ParentPersonId}:{first.ChildPersonId}", TreeIntegrityIssueType.DuplicateParentChildRelationship, TreeIntegritySeverity.High, "Duplicate parent-child relationship", "The same parent and child are connected more than once.", $"{group.Count()} relationship records connect the same people.", first.ChildPersonId, first.ParentPersonId, first.Id));
        }

        foreach (var link in parentLinks.Where(x => x.RelationshipType is ParentChildRelationshipType.Biological or ParentChildRelationshipType.Adoptive))
        {
            if (!byId.TryGetValue(link.ParentPersonId, out var parent) || !byId.TryGetValue(link.ChildPersonId, out var child))
                continue;

            if (parent.BirthDate.HasValue && child.BirthDate.HasValue)
            {
                if (parent.BirthDate.Value > child.BirthDate.Value)
                    Add(Finding($"ParentBornAfterChild:{link.Id}", TreeIntegrityIssueType.ParentBornAfterChild, TreeIntegritySeverity.Critical, "Parent is recorded as born after child", $"{parent.DisplayName} is linked as a parent of {child.DisplayName}, but the parent's birth date occurs after the child's birth date.", $"Parent birth {parent.BirthDate:yyyy-MM-dd}; child birth {child.BirthDate:yyyy-MM-dd}.", child.Id, parent.Id, link.Id));
                else
                {
                    var age = AgeAt(parent.BirthDate.Value, child.BirthDate.Value);
                    if (age < 12)
                        Add(Finding($"ParentTooYoung:{link.Id}", TreeIntegrityIssueType.ParentTooYoung, TreeIntegritySeverity.Critical, "Parent age at child birth is impossible or extremely improbable", $"{parent.DisplayName} would have been about {age} years old when {child.DisplayName} was born.", "The checker uses a conservative minimum parental age of 12 and flags for human review only.", child.Id, parent.Id, link.Id));
                    else if (age < 16)
                        Add(Finding($"ParentYoung:{link.Id}", TreeIntegrityIssueType.ParentTooYoung, TreeIntegritySeverity.High, "Parent age at child birth is unusually young", $"{parent.DisplayName} would have been about {age} years old when {child.DisplayName} was born.", "Verify birth dates and the relationship with original evidence.", child.Id, parent.Id, link.Id));
                    else if (age > 80)
                        Add(Finding($"ParentOld:{link.Id}", TreeIntegrityIssueType.ParentImplausiblyOld, TreeIntegritySeverity.High, "Parent age at child birth is unusually high", $"{parent.DisplayName} would have been about {age} years old when {child.DisplayName} was born.", "Because sex/biological role may be unknown, the checker uses a broad age threshold and does not change data.", child.Id, parent.Id, link.Id));
                }
            }

            if (parent.DeathDate.HasValue && child.BirthDate.HasValue && child.BirthDate.Value > parent.DeathDate.Value)
            {
                var days = child.BirthDate.Value.DayNumber - parent.DeathDate.Value.DayNumber;
                var type = days > 305 ? TreeIntegrityIssueType.ChildBornAfterParentDeath : TreeIntegrityIssueType.PossiblePosthumousBirth;
                var severity = days > 305 ? TreeIntegritySeverity.Critical : TreeIntegritySeverity.Medium;
                Add(Finding($"ChildAfterParentDeath:{link.Id}", type, severity, days > 305 ? "Child birth occurs more than ten months after parent death" : "Child birth occurs after parent death", $"{child.DisplayName} is recorded as born {days} day(s) after {parent.DisplayName}'s death.", days > 305 ? "This exceeds a conservative posthumous-birth window and requires review." : "A posthumous birth may be historically valid. Verify the biological relationship and dates before making any change.", child.Id, parent.Id, link.Id));
            }
        }

        var biologicalParentCounts = parentLinks.Where(x => x.RelationshipType == ParentChildRelationshipType.Biological).GroupBy(x => x.ChildPersonId).ToDictionary(g => g.Key, g => g.Select(x => x.ParentPersonId).Distinct().Count());
        foreach (var pair in biologicalParentCounts.Where(x => x.Value == 1))
            if (byId.TryGetValue(pair.Key, out var child))
                Add(Finding($"OneParent:{child.Id}", TreeIntegrityIssueType.OnlyOneParentLinked, TreeIntegritySeverity.Informational, "Only one biological parent is linked", $"{child.DisplayName} currently has one biological parent relationship.", "This may be complete for the available evidence; it is a research prompt, not an error.", child.Id));

        foreach (var cyclePersonId in FindCycleParticipants(parentLinks.Where(x => x.RelationshipType is ParentChildRelationshipType.Biological or ParentChildRelationshipType.Adoptive)))
            if (byId.TryGetValue(cyclePersonId, out var person))
                Add(Finding($"CircularAncestry:{cyclePersonId}", TreeIntegrityIssueType.CircularAncestry, TreeIntegritySeverity.Critical, "Circular ancestry detected", $"{person.DisplayName} participates in a parent-child path that loops back to an earlier person.", "A person cannot be their own ancestor. Review the relationships; the checker will not remove any relationship automatically.", person.Id));

        foreach (var relationship in coupleLinks)
        {
            if (!byId.TryGetValue(relationship.PersonAId, out var a) || !byId.TryGetValue(relationship.PersonBId, out var b))
                continue;
            if (relationship.StartDate.HasValue)
            {
                foreach (var person in new[] { a, b })
                {
                    if (person.BirthDate.HasValue && relationship.StartDate.Value < person.BirthDate.Value)
                        Add(Finding($"RelationshipBeforeBirth:{relationship.Id}:{person.Id}", TreeIntegrityIssueType.RelationshipStartBeforeBirth, TreeIntegritySeverity.Critical, "Relationship starts before a participant's birth", $"The {relationship.RelationshipType} relationship starts before {person.DisplayName}'s recorded birth date.", $"Relationship start {relationship.StartDate:yyyy-MM-dd}; birth {person.BirthDate:yyyy-MM-dd}.", person.Id, person.Id == a.Id ? b.Id : a.Id, relationship.Id));
                    if (person.DeathDate.HasValue && relationship.StartDate.Value > person.DeathDate.Value)
                        Add(Finding($"RelationshipAfterDeath:{relationship.Id}:{person.Id}", TreeIntegrityIssueType.RelationshipStartAfterDeath, TreeIntegritySeverity.Critical, "Relationship starts after a participant's death", $"The {relationship.RelationshipType} relationship starts after {person.DisplayName}'s recorded death date.", $"Relationship start {relationship.StartDate:yyyy-MM-dd}; death {person.DeathDate:yyyy-MM-dd}.", person.Id, person.Id == a.Id ? b.Id : a.Id, relationship.Id));
                }
            }
            if (relationship.StartDate.HasValue && relationship.EndDate.HasValue && relationship.EndDate.Value < relationship.StartDate.Value)
                Add(Finding($"RelationshipEndBeforeStart:{relationship.Id}", TreeIntegrityIssueType.RelationshipEndBeforeStart, TreeIntegritySeverity.Critical, "Relationship end date precedes start date", "A spouse/partner relationship has an end date earlier than its start date.", $"Start {relationship.StartDate:yyyy-MM-dd}; end {relationship.EndDate:yyyy-MM-dd}.", a.Id, b.Id, relationship.Id));
        }

        foreach (var burial in burials)
            if (byId.TryGetValue(burial.PersonId, out var person) && burial.BurialDate.HasValue && person.DeathDate.HasValue && burial.BurialDate.Value < person.DeathDate.Value)
                Add(Finding($"BurialBeforeDeath:{burial.Id}", TreeIntegrityIssueType.BurialBeforeDeath, TreeIntegritySeverity.Critical, "Burial date precedes death date", $"{person.DisplayName}'s burial date occurs before the recorded death date.", $"Burial {burial.BurialDate:yyyy-MM-dd}; death {person.DeathDate:yyyy-MM-dd}.", person.Id));

        foreach (var service in military)
        {
            if (!byId.TryGetValue(service.PersonId, out var person)) continue;
            if (service.StartDate.HasValue && person.BirthDate.HasValue && service.StartDate.Value < person.BirthDate.Value)
                Add(Finding($"MilitaryBeforeBirth:{service.Id}", TreeIntegrityIssueType.MilitaryServiceBeforeBirth, TreeIntegritySeverity.Critical, "Military service begins before birth", $"{person.DisplayName}'s military service starts before the recorded birth date.", $"Service start {service.StartDate:yyyy-MM-dd}; birth {person.BirthDate:yyyy-MM-dd}.", person.Id));
            if (service.StartDate.HasValue && person.DeathDate.HasValue && service.StartDate.Value > person.DeathDate.Value)
                Add(Finding($"MilitaryAfterDeath:{service.Id}", TreeIntegrityIssueType.MilitaryServiceAfterDeath, TreeIntegritySeverity.Critical, "Military service begins after death", $"{person.DisplayName}'s military service starts after the recorded death date.", $"Service start {service.StartDate:yyyy-MM-dd}; death {person.DeathDate:yyyy-MM-dd}.", person.Id));
            if (service.StartDate.HasValue && service.EndDate.HasValue && service.EndDate.Value < service.StartDate.Value)
                Add(Finding($"MilitaryEndBeforeStart:{service.Id}", TreeIntegrityIssueType.MilitaryEndBeforeStart, TreeIntegritySeverity.Critical, "Military service ends before it begins", $"{person.DisplayName}'s service end date occurs before the service start date.", $"Start {service.StartDate:yyyy-MM-dd}; end {service.EndDate:yyyy-MM-dd}.", person.Id));
        }

        foreach (var group in timeline.Where(x => x.EventDate.HasValue).GroupBy(x => $"{x.PersonId}:{NormalizeText(x.EventType)}:{NormalizeText(x.Title)}:{x.EventDate:yyyy-MM-dd}:{NormalizeText(x.PlaceName)}").Where(g => g.Count() > 1))
        {
            var first = group.First();
            Add(Finding($"DuplicateTimeline:{first.PersonId}:{NormalizeText(first.EventType)}:{first.EventDate:yyyyMMdd}", TreeIntegrityIssueType.DuplicateTimelineEvent, TreeIntegritySeverity.Medium, "Possible duplicate timeline event", "Multiple timeline events have the same type, title, date and place.", $"{group.Count()} matching events found: {first.Title} on {first.EventDate:yyyy-MM-dd}.", first.PersonId));
        }

        foreach (var group in timeline.Where(x => x.EventDate.HasValue).GroupBy(x => $"{x.PersonId}:{NormalizeText(x.EventType)}:{NormalizeText(x.Title)}").Where(g => g.Select(x => x.EventDate).Distinct().Count() > 1))
        {
            var first = group.First();
            var dates = string.Join(", ", group.Select(x => x.EventDate!.Value.ToString("yyyy-MM-dd")).Distinct().OrderBy(x => x));
            Add(Finding($"ConflictingTimeline:{first.PersonId}:{NormalizeText(first.EventType)}:{NormalizeText(first.Title)}", TreeIntegrityIssueType.ConflictingTimelineEvent, TreeIntegritySeverity.Medium, "Timeline event has conflicting dates", "The same normalized event type and title appears with more than one exact date.", $"{first.Title}: {dates}.", first.PersonId));
        }

        return findings.Values.OrderBy(x => x.Severity).ThenBy(x => x.IssueKey, StringComparer.Ordinal).ToList();
    }

    private static async Task SynchronizeIssuesAsync(FamilyTreeDbContext db, IReadOnlyList<TreeIntegrityFinding> findings, CancellationToken cancellationToken)
    {
        var existing = await db.TreeIntegrityIssues.ToDictionaryAsync(x => x.IssueKey, StringComparer.Ordinal, cancellationToken);
        var detectedKeys = findings.Select(x => x.IssueKey).ToHashSet(StringComparer.Ordinal);

        foreach (var finding in findings)
        {
            if (existing.TryGetValue(finding.IssueKey, out var issue))
                issue.Refresh(finding.Severity, finding.Title, finding.Description, finding.EvidenceSummary, finding.PersonId, finding.RelatedPersonId, finding.RelationshipId);
            else
                db.TreeIntegrityIssues.Add(new TreeIntegrityIssue(finding.IssueKey, finding.IssueType, finding.Severity, finding.Title, finding.Description, finding.EvidenceSummary, finding.PersonId, finding.RelatedPersonId, finding.RelationshipId));
        }

        foreach (var stale in existing.Values.Where(x => x.IsActive && !detectedKeys.Contains(x.IssueKey)))
            stale.MarkCleared();

        await db.SaveChangesAsync(cancellationToken);
    }

    private static TreeIntegrityIssueListItem ToListItem(TreeIntegrityIssue issue, IReadOnlyDictionary<Guid, Person> people)
    {
        people.TryGetValue(issue.PersonId ?? Guid.Empty, out var person);
        people.TryGetValue(issue.RelatedPersonId ?? Guid.Empty, out var related);
        return new TreeIntegrityIssueListItem(issue.Id, issue.IssueType, issue.Severity, issue.Status, issue.IsActive, issue.Title, issue.Description, issue.EvidenceSummary, issue.PersonId, person?.DisplayName ?? string.Empty, person is null ? string.Empty : Legacy(person), issue.RelatedPersonId, related?.DisplayName ?? string.Empty, related is null ? string.Empty : Legacy(related), issue.FirstDetectedUtc, issue.LastDetectedUtc, issue.ReviewedUtc, issue.ReviewedBy, issue.ReviewReason);
    }

    private static bool HasCitation(IEnumerable<CitationRecord> citations, Guid personId, string keyword) => citations.Any(x => x.PersonId == personId && x.FactOrSubject.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    private static string Legacy(Person person) => person.Identifiers.FirstOrDefault(x => x.IdentifierType == PersonIdentifierType.LegacyNumber)?.Value ?? string.Empty;

    private static int AgeAt(DateOnly birth, DateOnly later)
    {
        var age = later.Year - birth.Year;
        if (later.Month < birth.Month || (later.Month == birth.Month && later.Day < birth.Day)) age--;
        return age;
    }

    private static IReadOnlyCollection<Guid> FindCycleParticipants(IEnumerable<ParentChildRelationship> links)
    {
        var graph = links.GroupBy(x => x.ParentPersonId).ToDictionary(g => g.Key, g => g.Select(x => x.ChildPersonId).Distinct().ToList());
        var state = new Dictionary<Guid, int>();
        var stack = new List<Guid>();
        var cycle = new HashSet<Guid>();

        void Visit(Guid node)
        {
            if (state.TryGetValue(node, out var current))
            {
                if (current == 1)
                {
                    var start = stack.LastIndexOf(node);
                    if (start >= 0) foreach (var id in stack.Skip(start)) cycle.Add(id);
                }
                return;
            }
            state[node] = 1; stack.Add(node);
            if (graph.TryGetValue(node, out var children)) foreach (var child in children) Visit(child);
            stack.RemoveAt(stack.Count - 1); state[node] = 2;
        }

        foreach (var node in graph.Keys) Visit(node);
        return cycle;
    }

    private static TreeIntegrityFinding Finding(string key, TreeIntegrityIssueType type, TreeIntegritySeverity severity, string title, string description, string evidence, Guid? personId = null, Guid? relatedPersonId = null, Guid? relationshipId = null) => new(key, type, severity, title, description, evidence, personId, relatedPersonId, relationshipId);
    private static string NormalizeName(string value) => Regex.Replace((value ?? string.Empty).ToLowerInvariant(), "[^a-z0-9]+", string.Empty);
    private static string NormalizeText(string value) => Regex.Replace((value ?? string.Empty).ToLowerInvariant(), "\\s+", " ").Trim();

    internal sealed record TreeIntegrityFinding(string IssueKey, TreeIntegrityIssueType IssueType, TreeIntegritySeverity Severity, string Title, string Description, string EvidenceSummary, Guid? PersonId, Guid? RelatedPersonId, Guid? RelationshipId);
}
