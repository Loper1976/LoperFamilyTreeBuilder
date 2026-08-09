using System.Collections;
using System.Reflection;
using LoperFamilyTreeBuilder.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace LoperFamilyTreeBuilder.Data.Services;

/// <summary>
/// Builds a read-only archival report from the authoritative genealogy database.
/// The service never changes genealogy data or protected Legacy Numbers.
/// </summary>
public sealed class DetailedPersonReportService(IDbContextFactory<FamilyTreeDbContext> contextFactory)
{
    private static readonly string[] SensitivePropertyFragments =
    [
        "Password", "PasswordHash", "SecurityStamp", "ConcurrencyStamp", "Token", "Secret", "Credential"
    ];

    public async Task<DetailedPersonReport?> BuildAsync(
        Guid personId,
        DetailedReportMode mode = DetailedReportMode.Archival,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);

        // Materialize people first so relationship IDs can be rendered as names without
        // imposing assumptions on the historical schema beyond the existing People DbSet.
        var people = await db.People.AsNoTracking().ToListAsync(cancellationToken);
        var person = people.Cast<object>().FirstOrDefault(x => GetGuid(x, "Id", "PersonId") == personId);
        if (person is null)
            return null;

        var peopleById = people.Cast<object>()
            .Select(x => new { Person = x, Id = GetGuid(x, "Id", "PersonId") })
            .Where(x => x.Id.HasValue)
            .ToDictionary(x => x.Id!.Value, x => x.Person);

        var displayName = GetDisplayName(person);
        var isDeceased = DetermineDeceased(person);
        var birth = GetDateLike(person, "BirthDate", "DateOfBirth", "Born", "BirthYear");
        var death = GetDateLike(person, "DeathDate", "DateOfDeath", "Died", "DeathYear");
        var lifespan = BuildLifespan(birth, death);
        var branchName = GetString(person, "FamilyBranchName", "BranchName", "Branch");

        var vitalFacts = BuildVitalFacts(person);
        var relationships = new List<PersonReportRelationship>();
        var timeline = new List<PersonReportTimelineItem>();
        var sections = new Dictionary<string, List<PersonReportRecord>>(StringComparer.OrdinalIgnoreCase);
        string? legacyNumber = null;
        string? primaryPhoto = null;

        foreach (var entityType in db.Model.GetEntityTypes())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var clrType = entityType.ClrType;
            if (clrType is null || clrType == person.GetType())
                continue;

            var records = MaterializeEntitySet(db, clrType);
            foreach (var record in records)
            {
                if (!ReferencesPerson(record, personId))
                    continue;

                var typeName = clrType.Name;

                if (LooksLikeIdentifier(typeName, record))
                {
                    var kind = GetString(record, "IdentifierType", "Type", "Kind") ?? string.Empty;
                    if (kind.Contains("Legacy", StringComparison.OrdinalIgnoreCase))
                        legacyNumber ??= GetString(record, "Value", "Identifier", "Number");
                    continue;
                }

                if (LooksLikeParentChild(typeName))
                {
                    AddParentChildRelationship(record, personId, peopleById, relationships);
                    continue;
                }

                if (LooksLikeCouple(typeName))
                {
                    AddCoupleRelationship(record, personId, peopleById, relationships);
                    continue;
                }

                var category = Classify(typeName);

                if (category == "Timeline")
                {
                    timeline.Add(ToTimeline(record));
                    continue;
                }

                if (category == "Photos")
                {
                    primaryPhoto ??= FindUsableImageReference(record);
                }

                // Medical information is intentionally excluded for living people.
                // For deceased people it appears only in the Full Archival report.
                if (category == "Medical" && (!isDeceased || mode != DetailedReportMode.Archival))
                    continue;

                if (mode == DetailedReportMode.PublicSafe &&
                    category is "Medical" or "Research" or "Audit" or "Administration")
                    continue;

                if (mode == DetailedReportMode.Family && category is "Audit" or "Administration")
                    continue;

                if (!sections.TryGetValue(category, out var list))
                {
                    list = [];
                    sections[category] = list;
                }

                list.Add(ToReportRecord(record));
            }
        }

        // Some historical databases keep the Legacy Number directly on Person.
        legacyNumber ??= GetString(person, "LegacyNumber", "PedigreeNumber", "HistoricalNumber");
        primaryPhoto ??= FindUsableImageReference(person);

        // If timeline events are stored under a generic Events type, the classifier
        // places them in Timeline; sort dates when possible while preserving unknown dates.
        timeline = timeline
            .OrderBy(x => ParseSortableDate(x.Date) ?? DateTime.MaxValue)
            .ThenBy(x => x.Date)
            .ThenBy(x => x.Event)
            .ToList();

        var reportSections = sections
            .Select(x => new PersonReportSection(
                x.Key,
                SectionTitle(x.Key),
                x.Value,
                IsMedical: x.Key.Equals("Medical", StringComparison.OrdinalIgnoreCase),
                IsRestricted: x.Key is "Research" or "Audit" or "Administration"))
            .OrderBy(x => SectionOrder(x.Key))
            .ThenBy(x => x.Title)
            .ToList();

        return new DetailedPersonReport(
            PersonId: personId,
            DisplayName: displayName,
            LegacyNumber: legacyNumber,
            Lifespan: lifespan,
            IsDeceased: isDeceased,
            PrimaryPhotoReference: primaryPhoto,
            BranchName: branchName,
            GeneratedUtc: DateTimeOffset.UtcNow,
            VitalFacts: vitalFacts,
            FamilyRelationships: relationships
                .DistinctBy(x => new { x.Relationship, x.RelatedPersonId, x.PersonName })
                .OrderBy(x => RelationshipOrder(x.Relationship))
                .ThenBy(x => x.PersonName)
                .ToList(),
            Timeline: timeline,
            Sections: reportSections,
            Mode: mode);
    }

    private static IEnumerable<object> MaterializeEntitySet(DbContext db, Type clrType)
    {
        try
        {
            var setMethod = typeof(DbContext).GetMethods(BindingFlags.Instance | BindingFlags.Public)
                .First(m => m.Name == nameof(DbContext.Set) && m.IsGenericMethod && m.GetParameters().Length == 0)
                .MakeGenericMethod(clrType);
            var set = setMethod.Invoke(db, null);
            return set is IEnumerable enumerable ? enumerable.Cast<object>().ToList() : [];
        }
        catch
        {
            // A report should remain available even if an unmapped/keyless auxiliary type
            // cannot be enumerated. The authoritative database is never modified here.
            return [];
        }
    }

    private static bool ReferencesPerson(object record, Guid personId)
    {
        foreach (var property in record.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!property.CanRead)
                continue;

            var name = property.Name;
            if (!name.Contains("Person", StringComparison.OrdinalIgnoreCase) &&
                name is not ("ParentId" or "ChildId" or "SpouseId" or "PartnerId"))
                continue;

            var value = SafeGet(property, record);
            if (value is Guid guid && guid == personId)
                return true;
            if (value is Guid? nullableGuid && nullableGuid.HasValue && nullableGuid.Value == personId)
                return true;
        }

        return false;
    }

    private static IReadOnlyList<PersonReportFact> BuildVitalFacts(object person)
    {
        var facts = new List<PersonReportFact>();
        AddFact(facts, "Born", GetDateLike(person, "BirthDate", "DateOfBirth", "Born", "BirthYear"));
        AddFact(facts, "Birth place", GetString(person, "BirthPlace", "PlaceOfBirth", "BirthLocation"));
        AddFact(facts, "Died", GetDateLike(person, "DeathDate", "DateOfDeath", "Died", "DeathYear"));
        AddFact(facts, "Death place", GetString(person, "DeathPlace", "PlaceOfDeath", "DeathLocation"));
        AddFact(facts, "Burial", GetDateLike(person, "BurialDate", "IntermentDate"));
        AddFact(facts, "Burial place", GetString(person, "BurialPlace", "CemeteryName", "Cemetery"));
        AddFact(facts, "Occupation", GetString(person, "Occupation", "PrimaryOccupation"));
        AddFact(facts, "Religion", GetString(person, "Religion", "Faith", "Denomination"));
        return facts;
    }

    private static void AddParentChildRelationship(
        object record,
        Guid personId,
        IReadOnlyDictionary<Guid, object> people,
        ICollection<PersonReportRelationship> output)
    {
        var parentId = GetGuid(record, "ParentPersonId", "ParentId");
        var childId = GetGuid(record, "ChildPersonId", "ChildId");
        if (parentId == personId && childId.HasValue)
            output.Add(new("Child", NameFor(childId.Value, people), childId));
        else if (childId == personId && parentId.HasValue)
            output.Add(new("Parent", NameFor(parentId.Value, people), parentId));
    }

    private static void AddCoupleRelationship(
        object record,
        Guid personId,
        IReadOnlyDictionary<Guid, object> people,
        ICollection<PersonReportRelationship> output)
    {
        var ids = record.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.CanRead && p.PropertyType == typeof(Guid) &&
                        (p.Name.Contains("Person", StringComparison.OrdinalIgnoreCase) ||
                         p.Name.Contains("Spouse", StringComparison.OrdinalIgnoreCase) ||
                         p.Name.Contains("Partner", StringComparison.OrdinalIgnoreCase)))
            .Select(p => (Guid?)SafeGet(p, record))
            .Where(x => x.HasValue)
            .Select(x => x!.Value)
            .Distinct()
            .ToList();

        foreach (var other in ids.Where(x => x != personId))
            output.Add(new("Spouse / Partner", NameFor(other, people), other, GetDateLike(record, "MarriageDate", "StartDate")));
    }

    private static PersonReportTimelineItem ToTimeline(object record)
    {
        var eventName = GetString(record, "Title", "EventType", "Type", "Name") ?? Humanize(record.GetType().Name);
        var date = GetDateLike(record, "EventDate", "Date", "StartDate", "OccurredOn", "Year");
        var place = GetString(record, "Place", "Location", "PlaceName", "Address");
        var detail = GetString(record, "Description", "Details", "Notes", "Narrative", "Text");
        var source = GetString(record, "Source", "Citation", "Repository");
        return new(date, eventName, place, detail, source);
    }

    private static PersonReportRecord ToReportRecord(object record)
    {
        var facts = new List<PersonReportFact>();
        string? narrative = null;
        var type = record.GetType();

        foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!property.CanRead || property.GetIndexParameters().Length > 0)
                continue;
            if (SensitivePropertyFragments.Any(x => property.Name.Contains(x, StringComparison.OrdinalIgnoreCase)))
                continue;
            if (property.Name is "Id" or "PersonId" || property.Name.EndsWith("PersonId", StringComparison.OrdinalIgnoreCase))
                continue;
            if (typeof(IEnumerable).IsAssignableFrom(property.PropertyType) && property.PropertyType != typeof(string))
                continue;
            if (!IsSimple(property.PropertyType))
                continue;

            var value = SafeGet(property, record);
            if (value is null)
                continue;
            var text = FormatValue(value);
            if (string.IsNullOrWhiteSpace(text))
                continue;

            if (property.Name.Contains("Narrative", StringComparison.OrdinalIgnoreCase) ||
                property.Name.Contains("Biography", StringComparison.OrdinalIgnoreCase) ||
                property.Name.Equals("Description", StringComparison.OrdinalIgnoreCase) ||
                property.Name.Equals("Notes", StringComparison.OrdinalIgnoreCase))
            {
                if (text.Length > 80)
                {
                    narrative ??= text;
                    continue;
                }
            }

            facts.Add(new(Humanize(property.Name), text));
        }

        var heading = GetString(record, "Title", "Name", "ConditionName", "EventType", "Type", "Category")
                      ?? Humanize(type.Name);
        return new(heading, facts.Take(16).ToList(), narrative);
    }

    private static string Classify(string typeName)
    {
        if (ContainsAny(typeName, "Medical", "Health", "Condition", "Diagnosis", "Hospital", "Surgery", "Medication")) return "Medical";
        if (ContainsAny(typeName, "Military", "ServiceRecord", "Veteran", "Award", "UnitAssignment")) return "Military";
        if (ContainsAny(typeName, "Cemetery", "Burial", "Grave", "Headstone")) return "Cemetery";
        if (ContainsAny(typeName, "Timeline", "LifeEvent", "Event")) return "Timeline";
        if (ContainsAny(typeName, "Source", "Citation", "Evidence")) return "Sources";
        if (ContainsAny(typeName, "Photo", "Image", "Media")) return "Photos";
        if (ContainsAny(typeName, "Document", "Attachment", "FileRecord")) return "Documents";
        if (ContainsAny(typeName, "Story", "Biography", "Narrative", "Interview", "OralHistory")) return "Stories";
        if (ContainsAny(typeName, "Research", "Task", "Suggestion", "Hypothesis")) return "Research";
        if (ContainsAny(typeName, "Audit", "ChangeHistory")) return "Audit";
        if (ContainsAny(typeName, "User", "Permission", "Role")) return "Administration";
        return "Other Details";
    }

    private static bool LooksLikeIdentifier(string typeName, object record) =>
        typeName.Contains("Identifier", StringComparison.OrdinalIgnoreCase) ||
        record.GetType().GetProperty("IdentifierType") is not null;

    private static bool LooksLikeParentChild(string typeName) =>
        typeName.Contains("ParentChild", StringComparison.OrdinalIgnoreCase) ||
        (typeName.Contains("Relationship", StringComparison.OrdinalIgnoreCase) && typeName.Contains("Parent", StringComparison.OrdinalIgnoreCase));

    private static bool LooksLikeCouple(string typeName) =>
        typeName.Contains("Couple", StringComparison.OrdinalIgnoreCase) ||
        typeName.Contains("Spouse", StringComparison.OrdinalIgnoreCase) ||
        typeName.Contains("Marriage", StringComparison.OrdinalIgnoreCase);

    private static bool DetermineDeceased(object person)
    {
        var living = GetBool(person, "IsLiving", "Living");
        if (living.HasValue)
            return !living.Value;
        return !string.IsNullOrWhiteSpace(GetDateLike(person, "DeathDate", "DateOfDeath", "DeathYear", "Died"));
    }

    private static string GetDisplayName(object person)
    {
        var direct = GetString(person, "DisplayName", "FullName", "PreferredDisplayName");
        if (!string.IsNullOrWhiteSpace(direct))
            return direct;

        var parts = new[]
        {
            GetString(person, "GivenName", "FirstName"),
            GetString(person, "MiddleName", "MiddleNames"),
            GetString(person, "Surname", "LastName", "FamilyName"),
            GetString(person, "Suffix")
        }.Where(x => !string.IsNullOrWhiteSpace(x));
        var joined = string.Join(" ", parts!);
        return string.IsNullOrWhiteSpace(joined) ? "Unnamed Person" : joined;
    }

    private static string NameFor(Guid id, IReadOnlyDictionary<Guid, object> people) =>
        people.TryGetValue(id, out var person) ? GetDisplayName(person) : id.ToString();

    private static Guid? GetGuid(object target, params string[] names)
    {
        foreach (var name in names)
        {
            var p = FindProperty(target, name);
            var value = p is null ? null : SafeGet(p, target);
            if (value is Guid g) return g;
            if (value is string s && Guid.TryParse(s, out var parsed)) return parsed;
        }
        return null;
    }

    private static bool? GetBool(object target, params string[] names)
    {
        foreach (var name in names)
        {
            var p = FindProperty(target, name);
            var value = p is null ? null : SafeGet(p, target);
            if (value is bool b) return b;
            if (value is string s && bool.TryParse(s, out var parsed)) return parsed;
        }
        return null;
    }

    private static string? GetString(object target, params string[] names)
    {
        foreach (var name in names)
        {
            var p = FindProperty(target, name);
            var value = p is null ? null : SafeGet(p, target);
            if (value is null) continue;
            var text = FormatValue(value);
            if (!string.IsNullOrWhiteSpace(text)) return text;
        }
        return null;
    }

    private static string? GetDateLike(object target, params string[] names)
    {
        foreach (var name in names)
        {
            var p = FindProperty(target, name);
            var value = p is null ? null : SafeGet(p, target);
            if (value is null) continue;
            return FormatDateValue(value);
        }
        return null;
    }

    private static PropertyInfo? FindProperty(object target, string name) =>
        target.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .FirstOrDefault(x => x.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

    private static object? SafeGet(PropertyInfo property, object target)
    {
        try { return property.GetValue(target); }
        catch { return null; }
    }

    private static string FormatValue(object value) => value switch
    {
        DateTime dt => dt.ToString("MMM d, yyyy"),
        DateTimeOffset dto => dto.ToString("MMM d, yyyy"),
        DateOnly date => date.ToString("MMM d, yyyy"),
        bool b => b ? "Yes" : "No",
        Enum e => Humanize(e.ToString()),
        _ => value.ToString()?.Trim() ?? string.Empty
    };

    private static string FormatDateValue(object value) => value switch
    {
        DateTime dt => dt.ToString("MMM d, yyyy"),
        DateTimeOffset dto => dto.ToString("MMM d, yyyy"),
        DateOnly date => date.ToString("MMM d, yyyy"),
        int year when year > 0 => year.ToString(),
        long year when year > 0 && year < 10000 => year.ToString(),
        _ => FormatValue(value)
    };

    private static bool IsSimple(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        return underlying.IsPrimitive || underlying.IsEnum || underlying == typeof(string) ||
               underlying == typeof(Guid) || underlying == typeof(DateTime) ||
               underlying == typeof(DateTimeOffset) || underlying == typeof(DateOnly) ||
               underlying == typeof(decimal);
    }

    private static string? FindUsableImageReference(object record)
    {
        var reference = GetString(record, "PublicUrl", "Url", "RelativeUrl", "WebPath", "RelativePath", "FilePath", "StoragePath");
        if (string.IsNullOrWhiteSpace(reference)) return null;
        var lower = reference.ToLowerInvariant();
        if (!(lower.EndsWith(".jpg") || lower.EndsWith(".jpeg") || lower.EndsWith(".png") || lower.EndsWith(".webp") || lower.EndsWith(".gif")))
            return null;
        if (reference.StartsWith("http", StringComparison.OrdinalIgnoreCase) || reference.StartsWith("/") || reference.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            return reference;
        return null;
    }

    private static void AddFact(ICollection<PersonReportFact> facts, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)) facts.Add(new(label, value));
    }

    private static string? BuildLifespan(string? birth, string? death)
    {
        static string? Year(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var match = System.Text.RegularExpressions.Regex.Match(value, @"\b(1[0-9]{3}|20[0-9]{2})\b");
            return match.Success ? match.Value : value;
        }
        var b = Year(birth);
        var d = Year(death);
        if (b is null && d is null) return null;
        return $"{b ?? "?"} – {d ?? "Present"}";
    }

    private static DateTime? ParseSortableDate(string? value) =>
        DateTime.TryParse(value, out var result) ? result : null;

    private static bool ContainsAny(string value, params string[] terms) =>
        terms.Any(x => value.Contains(x, StringComparison.OrdinalIgnoreCase));

    private static string Humanize(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return value;
        var text = System.Text.RegularExpressions.Regex.Replace(value, "([a-z0-9])([A-Z])", "$1 $2");
        text = text.Replace("_", " ");
        return text.Trim();
    }

    private static string SectionTitle(string key) => key switch
    {
        "Medical" => "Medical & Family Health History",
        "Military" => "Military Service",
        "Cemetery" => "Cemetery & Burial",
        "Sources" => "Sources & Evidence",
        "Photos" => "Photographs",
        "Documents" => "Documents",
        "Stories" => "Stories & Biography",
        "Research" => "Research Notes & Open Questions",
        "Other Details" => "Other Details",
        _ => key
    };

    private static int SectionOrder(string key) => key switch
    {
        "Stories" => 10,
        "Military" => 20,
        "Cemetery" => 30,
        "Medical" => 40,
        "Documents" => 50,
        "Photos" => 60,
        "Sources" => 70,
        "Research" => 80,
        "Other Details" => 90,
        "Audit" => 100,
        "Administration" => 110,
        _ => 95
    };

    private static int RelationshipOrder(string relationship) => relationship switch
    {
        "Parent" => 10,
        "Spouse / Partner" => 20,
        "Child" => 30,
        _ => 40
    };
}
