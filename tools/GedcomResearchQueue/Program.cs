using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LoperFamilyTreeBuilder.ImportExport.Gedcom;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: GedcomResearchQueue <input.ged> <output.json>");
    return 2;
}

var inputPath = Path.GetFullPath(args[0]);
var outputPath = Path.GetFullPath(args[1]);
if (!File.Exists(inputPath))
{
    Console.Error.WriteLine("GEDCOM input does not exist.");
    return 2;
}

var bytes = await File.ReadAllBytesAsync(inputPath);
var sha256 = Convert.ToHexString(SHA256.HashData(bytes));
await using var stream = new MemoryStream(bytes, writable: false);
var document = await new GedcomParser().ParseAsync(stream);
if (!document.IsStructurallyValid)
{
    Console.Error.WriteLine("GEDCOM is structurally invalid; no queue was written.");
    foreach (var diagnostic in document.Diagnostics.Take(20)) Console.Error.WriteLine(diagnostic);
    return 3;
}

var connected = document.Families
    .SelectMany(family => new[] { family.HusbandId, family.WifeId }
        .Where(id => id is not null).Cast<string>().Concat(family.ChildIds))
    .ToHashSet(StringComparer.Ordinal);

var items = document.Individuals
    .Where(person => person.Privacy != GedcomPrivacyClassification.ProtectedUncertain)
    .Select(person => CreateItem(person, connected.Contains(person.ExternalId)))
    .OrderByDescending(item => item.PriorityScore)
    .ThenBy(item => item.ExternalPersonId, StringComparer.Ordinal)
    .ToArray();

var queue = new ResearchQueueSnapshot(
    SchemaVersion: "1.0",
    Source: new QueueSource(Path.GetFileName(inputPath), sha256, document.Version),
    GeneratedUtc: DateTimeOffset.UtcNow,
    Policy: "Deceased/public-historical only. Protected/uncertain people are excluded. Candidates require evidence review before accepted-tree promotion.",
    EligibleCount: items.Length,
    ProtectedExcludedCount: document.Individuals.Count - items.Length,
    Items: items);

Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
var temporaryPath = outputPath + ".tmp-" + Guid.NewGuid().ToString("N");
var options = new JsonSerializerOptions
{
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
};
await File.WriteAllTextAsync(temporaryPath, JsonSerializer.Serialize(queue, options) + Environment.NewLine,
    new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
File.Move(temporaryPath, outputPath, overwrite: true);
Console.WriteLine($"Wrote {items.Length} eligible research items; excluded {queue.ProtectedExcludedCount} protected/uncertain people.");
return 0;

static ResearchQueueItem CreateItem(GedcomIndividual person, bool hasFamilyLinks)
{
    var questions = new List<string>();
    var score = person.Privacy == GedcomPrivacyClassification.ExplicitlyDeceased ? 130 : 110;
    if (person.BirthDate is null) { questions.Add("Find and verify birth date and place."); score += 24; }
    else if (string.IsNullOrWhiteSpace(person.BirthPlace)) { questions.Add("Find and verify birth place."); score += 12; }
    if (!person.HasDeathRecord) { questions.Add("Find and verify death and burial evidence."); score += 22; }
    else if (string.IsNullOrWhiteSpace(person.DeathPlace)) { questions.Add("Find and verify death place and burial."); score += 12; }
    if (hasFamilyLinks) { questions.Add("Verify parent, spouse, and child relationships with independent records."); score += 8; }
    questions.Add("Search census, vital, military, immigration, land, probate, newspaper, and archival collections as applicable.");

    return new ResearchQueueItem(
        ResearchItemId: "RES-A-" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(person.ExternalId)))[..16],
        ExternalPersonId: person.ExternalId,
        GivenName: person.GivenName,
        Surname: person.Surname,
        PrivacyClass: person.Privacy.ToString(),
        PriorityScore: score,
        Status: "Queued",
        Birth: person.BirthDate?.OriginalText,
        BirthPlace: person.BirthPlace,
        Death: person.DeathDate?.OriginalText,
        DeathPlace: person.DeathPlace,
        HasFamilyLinks: hasFamilyLinks,
        Questions: questions);
}

internal sealed record ResearchQueueSnapshot(
    string SchemaVersion, QueueSource Source, DateTimeOffset GeneratedUtc, string Policy,
    int EligibleCount, int ProtectedExcludedCount, IReadOnlyList<ResearchQueueItem> Items);
internal sealed record QueueSource(string FileName, string Sha256, string GedcomVersion);
internal sealed record ResearchQueueItem(
    string ResearchItemId, string ExternalPersonId, string GivenName, string Surname,
    string PrivacyClass, int PriorityScore, string Status, string? Birth, string? BirthPlace,
    string? Death, string? DeathPlace, bool HasFamilyLinks, IReadOnlyList<string> Questions);
