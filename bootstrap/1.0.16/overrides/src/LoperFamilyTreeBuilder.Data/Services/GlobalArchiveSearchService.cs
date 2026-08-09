using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using LoperFamilyTreeBuilder.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace LoperFamilyTreeBuilder.Data.Services;

/// <summary>
/// Read-only cross-archive search and research-quality analysis.
/// Search results never alter authoritative genealogy or Legacy Numbers.
/// </summary>
public sealed class GlobalArchiveSearchService(
    IDbContextFactory<FamilyTreeDbContext> contextFactory,
    ResearchIntelligenceStore intelligenceStore)
{
    private static readonly string[] SensitivePropertyFragments =
    ["Password", "PasswordHash", "SecurityStamp", "ConcurrencyStamp", "Token", "Secret", "Credential"];

    public async Task<IReadOnlyList<ArchiveSearchHit>> SearchAsync(
        ArchiveSearchRequest request,
        int maxResults = 250,
        CancellationToken cancellationToken = default)
    {
        var query = request.Query?.Trim();
        var legacyQuery = request.LegacyNumber?.Trim();
        if (string.IsNullOrWhiteSpace(query) && string.IsNullOrWhiteSpace(legacyQuery))
            return [];

        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var people = await db.People.AsNoTracking().ToListAsync(cancellationToken);
        var peopleById = people.Cast<object>()
            .Select(x => new { Person = x, Id = GetGuid(x, "Id", "PersonId") })
            .Where(x => x.Id.HasValue)
            .ToDictionary(x => x.Id!.Value, x => x.Person);

        var legacyMap = BuildLegacyMap(db);
        var hits = new List<ArchiveSearchHit>();

        foreach (var person in people.Cast<object>())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var id = GetGuid(person, "Id", "PersonId");
            if (!id.HasValue) continue;
            var name = DisplayName(person);
            var legacy = legacyMap.GetValueOrDefault(id.Value) ?? GetString(person, "LegacyNumber", "PedigreeNumber", "HistoricalNumber");
            var matched = Matches(query, name) || Matches(legacyQuery, legacy) ||
                          person.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                              .Where(IsSearchableProperty)
                              .Select(p => SafeGet(p, person))
                              .Where(v => v is not null)
                              .Any(v => Matches(query, FormatValue(v!)) || Matches(legacyQuery, FormatValue(v!)));
            if (!matched) continue;

            var snippet = BuildPersonSnippet(person, legacy);
            hits.Add(new("People", name, snippet, id, name, legacy, $"/people/{id}", person.GetType().Name, 10));
            if (hits.Count >= maxResults) break;
        }

        if (hits.Count < maxResults)
        {
            foreach (var entityType in db.Model.GetEntityTypes())
            {
                cancellationToken.ThrowIfCancellationRequested();
                var clrType = entityType.ClrType;
                if (clrType is null || clrType == people.FirstOrDefault()?.GetType()) continue;
                var category = CategoryFor(clrType.Name);
                if (category == "Administration") continue;

                foreach (var record in MaterializeEntitySet(db, clrType))
                {
                    var match = FindMatch(record, query, legacyQuery);
                    if (match is null) continue;

                    var personId = FindAssociatedPersonId(record, peopleById.Keys);
                    var personName = personId.HasValue && peopleById.TryGetValue(personId.Value, out var person)
                        ? DisplayName(person)
                        : null;
                    var legacy = personId.HasValue ? legacyMap.GetValueOrDefault(personId.Value) : null;
                    var title = RecordTitle(record);
                    var snippet = $"{Humanize(match.Value.Property)}: {Truncate(match.Value.Value, 220)}";
                    hits.Add(new(category, title, snippet, personId, personName, legacy,
                        personId.HasValue ? $"/people/{personId}" : null, clrType.Name,
                        category == "Sources & Evidence" ? 6 : 4));
                    if (hits.Count >= maxResults) break;
                }

                if (hits.Count >= maxResults) break;
            }
        }

        var state = await intelligenceStore.LoadAsync(cancellationToken);
        if (hits.Count < maxResults && !string.IsNullOrWhiteSpace(query))
        {
            foreach (var alias in state.Aliases.Where(x => Matches(query, x.Alias) || Matches(query, x.AliasType)))
            {
                var personName = peopleById.TryGetValue(alias.PersonId, out var person) ? DisplayName(person) : "Person";
                hits.Add(new("Name Aliases", alias.Alias, $"{alias.AliasType} for {personName}", alias.PersonId,
                    personName, legacyMap.GetValueOrDefault(alias.PersonId), $"/people/{alias.PersonId}", nameof(PersonNameAliasRecord), 9));
                if (hits.Count >= maxResults) break;
            }

            foreach (var proof in state.ProofWorkspaces.Where(x => Matches(query, x.ResearchQuestion) || Matches(query, x.Conclusion) || Matches(query, x.ResearcherReasoning)))
            {
                var personName = proof.PersonId.HasValue && peopleById.TryGetValue(proof.PersonId.Value, out var person) ? DisplayName(person) : null;
                hits.Add(new("Proof Workspaces", proof.ResearchQuestion, proof.Conclusion ?? proof.Status,
                    proof.PersonId, personName, proof.PersonId.HasValue ? legacyMap.GetValueOrDefault(proof.PersonId.Value) : null,
                    "/research-intelligence#proof", nameof(ProofWorkspaceRecord), 8));
                if (hits.Count >= maxResults) break;
            }

            foreach (var repository in state.RepositorySearches.Where(x => Matches(query, x.Repository) || Matches(query, x.Collection) || Matches(query, x.SearchQuery) || Matches(query, x.Results) || Matches(query, x.Notes)))
            {
                var personName = repository.PersonId.HasValue && peopleById.TryGetValue(repository.PersonId.Value, out var person) ? DisplayName(person) : null;
                hits.Add(new("Repository Searches", repository.Repository,
                    repository.NegativeSearch ? $"Negative search: {repository.SearchQuery}" : repository.SearchQuery,
                    repository.PersonId, personName, repository.PersonId.HasValue ? legacyMap.GetValueOrDefault(repository.PersonId.Value) : null,
                    "/research-intelligence#repositories", nameof(RepositorySearchRecord), 7));
                if (hits.Count >= maxResults) break;
            }
        }

        return hits
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Category)
            .ThenBy(x => x.Title)
            .Take(maxResults)
            .ToList();
    }

    public async Task<IReadOnlyList<PersonAdvancedSearchHit>> SearchPeopleAsync(
        ArchiveSearchRequest request,
        int maxResults = 500,
        CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var people = await db.People.AsNoTracking().ToListAsync(cancellationToken);
        var personObjects = people.Cast<object>().ToList();
        var ids = personObjects.Select(x => GetGuid(x, "Id", "PersonId")).Where(x => x.HasValue).Select(x => x!.Value).ToHashSet();
        var legacyMap = BuildLegacyMap(db);
        var relationIndex = BuildRelationIndex(db, ids);
        var state = await intelligenceStore.LoadAsync(cancellationToken);
        var aliases = state.Aliases.GroupBy(x => x.PersonId).ToDictionary(x => x.Key, x => x.Select(a => a.Alias).ToList());
        var results = new List<PersonAdvancedSearchHit>();

        foreach (var person in personObjects)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var id = GetGuid(person, "Id", "PersonId");
            if (!id.HasValue) continue;
            var name = DisplayName(person);
            var legacy = legacyMap.GetValueOrDefault(id.Value) ?? GetString(person, "LegacyNumber", "PedigreeNumber", "HistoricalNumber");
            var living = IsLiving(person);
            var birthYear = YearOf(person, "BirthDate", "DateOfBirth", "BirthYear", "Born");
            var deathYear = YearOf(person, "DeathDate", "DateOfDeath", "DeathYear", "Died");
            var branch = GetString(person, "FamilyBranchName", "BranchName", "Branch");
            var related = relationIndex.GetValueOrDefault(id.Value) ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var personAliases = aliases.GetValueOrDefault(id.Value) ?? [];

            if (!string.IsNullOrWhiteSpace(request.Query) &&
                !Matches(request.Query, name) && !Matches(request.Query, legacy) &&
                !personAliases.Any(x => Matches(request.Query, x)) &&
                !PersonPropertyMatch(person, request.Query)) continue;
            if (!string.IsNullOrWhiteSpace(request.LegacyNumber) && !Matches(request.LegacyNumber, legacy)) continue;
            if (!string.IsNullOrWhiteSpace(request.Branch) && !Matches(request.Branch, branch)) continue;
            if (request.Living == LivingFilter.Living && !living) continue;
            if (request.Living == LivingFilter.Deceased && living) continue;
            if (request.BirthYearFrom.HasValue && (!birthYear.HasValue || birthYear < request.BirthYearFrom)) continue;
            if (request.BirthYearTo.HasValue && (!birthYear.HasValue || birthYear > request.BirthYearTo)) continue;
            if (request.DeathYearFrom.HasValue && (!deathYear.HasValue || deathYear < request.DeathYearFrom)) continue;
            if (request.DeathYearTo.HasValue && (!deathYear.HasValue || deathYear > request.DeathYearTo)) continue;
            if (request.MissingParent && related.Contains("Parent")) continue;
            if (request.MissingSource && related.Contains("Source")) continue;
            if (request.MissingCemetery && related.Contains("Cemetery")) continue;
            if (request.HasMilitary && !related.Contains("Military")) continue;
            if (request.HasMedical && !related.Contains("Medical")) continue;

            var flags = new List<string>();
            if (!birthYear.HasValue) flags.Add("Birth date missing");
            if (!living && !deathYear.HasValue) flags.Add("Death date missing");
            if (!related.Contains("Parent")) flags.Add("Parent research needed");
            if (!related.Contains("Source")) flags.Add("Source gap");
            if (!living && !related.Contains("Cemetery")) flags.Add("Cemetery research needed");
            if (!related.Contains("Photo")) flags.Add("Primary photo missing");

            results.Add(new(id.Value, name, legacy, Lifespan(birthYear, deathYear, living), branch, living, flags));
            if (results.Count >= maxResults) break;
        }

        return results.OrderBy(x => x.DisplayName).ToList();
    }

    public async Task<ResearchQualitySummary> GetQualitySummaryAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        var people = await db.People.AsNoTracking().ToListAsync(cancellationToken);
        var personObjects = people.Cast<object>().ToList();
        var ids = personObjects.Select(x => GetGuid(x, "Id", "PersonId")).Where(x => x.HasValue).Select(x => x!.Value).ToHashSet();
        var relationIndex = BuildRelationIndex(db, ids);
        var legacyMap = BuildLegacyMap(db);
        var state = await intelligenceStore.LoadAsync(cancellationToken);
        var issues = new List<DataQualityIssue>();
        var missingBirth=0;var deceasedMissingDeath=0;var missingParent=0;var missingSources=0;var missingCemetery=0;var missingPhoto=0;

        foreach (var person in personObjects)
        {
            var id=GetGuid(person,"Id","PersonId"); if(!id.HasValue) continue;
            var name=DisplayName(person);var living=IsLiving(person);var birth=YearOf(person,"BirthDate","DateOfBirth","BirthYear","Born");var death=YearOf(person,"DeathDate","DateOfDeath","DeathYear","Died");
            var related=relationIndex.GetValueOrDefault(id.Value)??new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            void Add(string severity,string category,string message){if(issues.Count<300)issues.Add(new(id,name,severity,category,message,$"/people/{id}"));}
            if(!birth.HasValue){missingBirth++;Add("Warning","Vital data","Birth date is missing.");}
            if(!living&&!death.HasValue){deceasedMissingDeath++;Add("Warning","Vital data","Person is marked deceased but no death date was found.");}
            if(!related.Contains("Parent")){missingParent++;Add("Research","Relationships","No parent relationship is recorded.");}
            if(!related.Contains("Source")){missingSources++;Add("Research","Sources","No linked source or citation was found.");}
            if(!living&&!related.Contains("Cemetery")){missingCemetery++;Add("Research","Cemetery","No cemetery or burial record was found.");}
            if(!related.Contains("Photo")){missingPhoto++;Add("Notice","Media","No linked photograph was found.");}
        }

        var duplicateLegacy = legacyMap
            .Where(x => !string.IsNullOrWhiteSpace(x.Value))
            .GroupBy(x => x.Value, StringComparer.OrdinalIgnoreCase)
            .Count(x => x.Count() > 1);
        if(duplicateLegacy>0) issues.Insert(0,new(null,null,"Critical","Preservation",$"{duplicateLegacy} duplicate Legacy Number value(s) require human review. No Legacy Numbers were changed."));

        return new ResearchQualitySummary(
            personObjects.Count, missingBirth, deceasedMissingDeath, missingParent, missingSources,
            missingCemetery, missingPhoto, duplicateLegacy,
            state.ProofWorkspaces.Count(x => !x.Status.Equals("Resolved",StringComparison.OrdinalIgnoreCase) && !x.Status.Equals("Archived",StringComparison.OrdinalIgnoreCase)),
            state.RepositorySearches.Count,
            issues);
    }

    private static Dictionary<Guid,string> BuildLegacyMap(DbContext db)
    {
        var map=new Dictionary<Guid,string>();
        foreach(var entityType in db.Model.GetEntityTypes().Where(x => x.ClrType?.Name.Contains("Identifier",StringComparison.OrdinalIgnoreCase)==true))
        {
            foreach(var record in MaterializeEntitySet(db,entityType.ClrType!))
            {
                var kind=GetString(record,"IdentifierType","Type","Kind")??string.Empty;
                if(!kind.Contains("Legacy",StringComparison.OrdinalIgnoreCase))continue;
                var personId=GetGuid(record,"PersonId");var value=GetString(record,"Value","Identifier","Number");
                if(personId.HasValue&&!string.IsNullOrWhiteSpace(value))map[personId.Value]=value;
            }
        }
        return map;
    }

    private static Dictionary<Guid,HashSet<string>> BuildRelationIndex(DbContext db,HashSet<Guid> peopleIds)
    {
        var index=peopleIds.ToDictionary(x=>x,_=>new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        foreach(var entityType in db.Model.GetEntityTypes())
        {
            var type=entityType.ClrType;if(type is null)continue;
            var category=CategoryFor(type.Name);
            foreach(var record in MaterializeEntitySet(db,type))
            {
                var linked=AllAssociatedPersonIds(record,peopleIds).ToList();
                foreach(var id in linked)
                {
                    if(!index.TryGetValue(id,out var set))continue;
                    set.Add(category switch
                    {
                        "Sources & Evidence"=>"Source",
                        "Cemetery & Burial"=>"Cemetery",
                        "Military"=>"Military",
                        "Medical & Health"=>"Medical",
                        "Photos"=>"Photo",
                        "Family Relationships"=>InferFamilyRelation(record,id),
                        _=>category
                    });
                }
            }
        }
        return index;
    }

    private static string InferFamilyRelation(object record,Guid id)
    {
        var parent=GetGuid(record,"ParentPersonId","ParentId");var child=GetGuid(record,"ChildPersonId","ChildId");
        if(child==id&&parent.HasValue)return "Parent";
        if(parent==id&&child.HasValue)return "Child";
        return "Spouse";
    }

    private static IEnumerable<object> MaterializeEntitySet(DbContext db,Type clrType)
    {
        try
        {
            var method=typeof(DbContext).GetMethods(BindingFlags.Instance|BindingFlags.Public)
                .First(m=>m.Name==nameof(DbContext.Set)&&m.IsGenericMethod&&m.GetParameters().Length==0).MakeGenericMethod(clrType);
            var set=method.Invoke(db,null);
            return set is IEnumerable enumerable?enumerable.Cast<object>().Take(10000).ToList():[];
        }
        catch{return [];}
    }

    private static (string Property,string Value)? FindMatch(object record,string? query,string? legacyQuery)
    {
        foreach(var p in record.GetType().GetProperties(BindingFlags.Instance|BindingFlags.Public).Where(IsSearchableProperty))
        {
            var value=SafeGet(p,record);if(value is null)continue;var text=FormatValue(value);
            if(Matches(query,text)||Matches(legacyQuery,text))return(p.Name,text);
        }
        return null;
    }

    private static bool IsSearchableProperty(PropertyInfo property)
    {
        if(!property.CanRead||property.GetIndexParameters().Length>0)return false;
        if(SensitivePropertyFragments.Any(x=>property.Name.Contains(x,StringComparison.OrdinalIgnoreCase)))return false;
        var type=Nullable.GetUnderlyingType(property.PropertyType)??property.PropertyType;
        return type==typeof(string)||type==typeof(Guid)||type==typeof(DateTime)||type==typeof(DateTimeOffset)||type==typeof(DateOnly)||type.IsPrimitive||type.IsEnum||type==typeof(decimal);
    }

    private static Guid? FindAssociatedPersonId(object record,IEnumerable<Guid> knownIds)=>AllAssociatedPersonIds(record,knownIds.ToHashSet()).FirstOrDefault() is var x&&x!=Guid.Empty?x:null;

    private static IEnumerable<Guid> AllAssociatedPersonIds(object record,HashSet<Guid> knownIds)
    {
        foreach(var p in record.GetType().GetProperties(BindingFlags.Instance|BindingFlags.Public))
        {
            if(!p.CanRead)continue;
            if(!(p.Name.Contains("Person",StringComparison.OrdinalIgnoreCase)||p.Name is "ParentId" or "ChildId" or "SpouseId" or "PartnerId"))continue;
            var value=SafeGet(p,record);
            if(value is Guid id&&knownIds.Contains(id))yield return id;
        }
    }

    private static string CategoryFor(string typeName)
    {
        if(ContainsAny(typeName,"Medical","Health","Condition","Diagnosis","Hospital","Surgery","Medication"))return "Medical & Health";
        if(ContainsAny(typeName,"Military","Veteran","Award","UnitAssignment","ServiceRecord"))return "Military";
        if(ContainsAny(typeName,"Cemetery","Burial","Grave","Headstone"))return "Cemetery & Burial";
        if(ContainsAny(typeName,"Source","Citation","Evidence"))return "Sources & Evidence";
        if(ContainsAny(typeName,"Photo","Image","Media"))return "Photos";
        if(ContainsAny(typeName,"Document","Attachment","FileRecord"))return "Documents";
        if(ContainsAny(typeName,"Story","Biography","Narrative","Interview","OralHistory"))return "Stories";
        if(ContainsAny(typeName,"Timeline","Event"))return "Timeline";
        if(ContainsAny(typeName,"Research","Task","Suggestion","Hypothesis"))return "Research";
        if(ContainsAny(typeName,"ParentChild","Couple","Spouse","Relationship","Marriage"))return "Family Relationships";
        if(ContainsAny(typeName,"Audit","ChangeHistory"))return "Audit";
        if(ContainsAny(typeName,"User","Permission","Role"))return "Administration";
        if(ContainsAny(typeName,"Identifier"))return "Identifiers";
        return "Archive Records";
    }

    private static string RecordTitle(object record)=>GetString(record,"Title","Name","DisplayName","ConditionName","EventType","Repository","FileName")??Humanize(record.GetType().Name);
    private static string BuildPersonSnippet(object person,string? legacy)
    {
        var birth=GetDateLike(person,"BirthDate","DateOfBirth","BirthYear","Born");var death=GetDateLike(person,"DeathDate","DateOfDeath","DeathYear","Died");var place=GetString(person,"BirthPlace","BirthLocation","PlaceOfBirth");
        return string.Join(" · ",new[]{legacy is null?null:$"Legacy {legacy}",birth is null?null:$"Born {birth}",death is null?null:$"Died {death}",place}.Where(x=>!string.IsNullOrWhiteSpace(x)));
    }
    private static bool PersonPropertyMatch(object person,string query)=>person.GetType().GetProperties(BindingFlags.Instance|BindingFlags.Public).Where(IsSearchableProperty).Select(p=>SafeGet(p,person)).Where(v=>v is not null).Any(v=>Matches(query,FormatValue(v!)));
    private static bool IsLiving(object person){var value=GetBool(person,"IsLiving","Living");if(value.HasValue)return value.Value;return string.IsNullOrWhiteSpace(GetDateLike(person,"DeathDate","DateOfDeath","DeathYear","Died"));}
    private static int? YearOf(object target,params string[] names){var text=GetDateLike(target,names);if(string.IsNullOrWhiteSpace(text))return null;var m=Regex.Match(text,@"\b(1[0-9]{3}|20[0-9]{2})\b");return m.Success&&int.TryParse(m.Value,out var y)?y:null;}
    private static string? Lifespan(int? birth,int? death,bool living)=>birth.HasValue||death.HasValue?$"{(birth?.ToString()??"?")} – {(living?"Present":death?.ToString()??"?")}":null;
    private static bool Matches(string? query,string? value)=>!string.IsNullOrWhiteSpace(query)&&!string.IsNullOrWhiteSpace(value)&&value.Contains(query.Trim(),StringComparison.OrdinalIgnoreCase);
    private static bool ContainsAny(string value,params string[] terms)=>terms.Any(x=>value.Contains(x,StringComparison.OrdinalIgnoreCase));
    private static string Truncate(string value,int length)=>value.Length<=length?value:value[..length]+"…";
    private static string DisplayName(object person){var direct=GetString(person,"DisplayName","FullName","PreferredDisplayName");if(!string.IsNullOrWhiteSpace(direct))return direct;var parts=new[]{GetString(person,"GivenName","FirstName"),GetString(person,"MiddleName","MiddleNames"),GetString(person,"Surname","LastName","FamilyName"),GetString(person,"Suffix")}.Where(x=>!string.IsNullOrWhiteSpace(x)).Select(x=>x!);var text=string.Join(" ",parts);return string.IsNullOrWhiteSpace(text)?"Unnamed Person":text;}
    private static Guid? GetGuid(object target,params string[] names){foreach(var n in names){var p=FindProperty(target,n);var v=p is null?null:SafeGet(p,target);if(v is Guid g)return g;if(v is string s&&Guid.TryParse(s,out var parsed))return parsed;}return null;}
    private static bool? GetBool(object target,params string[] names){foreach(var n in names){var p=FindProperty(target,n);var v=p is null?null:SafeGet(p,target);if(v is bool b)return b;if(v is string s&&bool.TryParse(s,out var parsed))return parsed;}return null;}
    private static string? GetString(object target,params string[] names){foreach(var n in names){var p=FindProperty(target,n);var v=p is null?null:SafeGet(p,target);if(v is null)continue;var text=FormatValue(v);if(!string.IsNullOrWhiteSpace(text))return text;}return null;}
    private static string? GetDateLike(object target,params string[] names){foreach(var n in names){var p=FindProperty(target,n);var v=p is null?null:SafeGet(p,target);if(v is null)continue;return v switch{DateTime dt=>dt.ToString("MMM d, yyyy"),DateTimeOffset dto=>dto.ToString("MMM d, yyyy"),DateOnly d=>d.ToString("MMM d, yyyy"),_=>FormatValue(v)};}return null;}
    private static PropertyInfo? FindProperty(object target,string name)=>target.GetType().GetProperties(BindingFlags.Instance|BindingFlags.Public).FirstOrDefault(x=>x.Name.Equals(name,StringComparison.OrdinalIgnoreCase));
    private static object? SafeGet(PropertyInfo p,object target){try{return p.GetValue(target);}catch{return null;}}
    private static string FormatValue(object value)=>value switch{DateTime dt=>dt.ToString("MMM d, yyyy"),DateTimeOffset dto=>dto.ToString("MMM d, yyyy"),DateOnly d=>d.ToString("MMM d, yyyy"),bool b=>b?"Yes":"No",Enum e=>Humanize(e.ToString()),_=>value.ToString()?.Trim()??string.Empty};
    private static string Humanize(string value)=>Regex.Replace(value.Replace("_"," "),"([a-z0-9])([A-Z])","$1 $2").Trim();
}
