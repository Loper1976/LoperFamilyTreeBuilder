using System.Globalization;

namespace LoperFamilyTreeBuilder.ImportExport.Gedcom;

public sealed class GedcomParser
{
    public async Task<GedcomDocument> ParseAsync(
        Stream stream,
        DateOnly? evaluationDate = null,
        CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(stream, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var lines = new List<GedcomLine>();
        while (await reader.ReadLineAsync(cancellationToken) is { } text)
        {
            if (TryParseLine(text, out var line)) lines.Add(line);
        }

        return Parse(lines, evaluationDate ?? DateOnly.FromDateTime(DateTime.UtcNow));
    }

    internal static GedcomDocument Parse(IReadOnlyList<GedcomLine> lines, DateOnly evaluationDate)
    {
        var diagnostics = new List<string>();
        if (lines.Count == 0 || lines[0].Level != 0 || lines[0].Tag != "HEAD")
            diagnostics.Add("GEDCOM header is missing.");
        if (lines.Count == 0 || lines[^1].Level != 0 || lines[^1].Tag != "TRLR")
            diagnostics.Add("GEDCOM trailer is missing.");

        var version = FindHeaderValue(lines, "GEDC", "VERS") ?? string.Empty;
        var characterEncoding = lines.FirstOrDefault(x => x.Level == 1 && x.Tag == "CHAR")?.Value ?? string.Empty;
        if (string.IsNullOrWhiteSpace(version)) diagnostics.Add("GEDCOM standard version is missing.");
        if (string.IsNullOrWhiteSpace(characterEncoding)) diagnostics.Add("GEDCOM character encoding is missing.");

        var individuals = new List<GedcomIndividual>();
        var families = new List<GedcomFamily>();
        var sourceCount = 0;
        var mediaCount = 0;

        foreach (var record in SplitRecords(lines))
        {
            switch (record.Header.Tag)
            {
                case "INDI" when record.Header.Xref is not null:
                    individuals.Add(ParseIndividual(record, evaluationDate));
                    break;
                case "FAM" when record.Header.Xref is not null:
                    families.Add(ParseFamily(record));
                    break;
                case "SOUR" when record.Header.Xref is not null:
                    sourceCount++;
                    break;
                case "OBJE" when record.Header.Xref is not null:
                    mediaCount++;
                    break;
            }
        }

        var individualIds = individuals.Select(x => x.ExternalId).ToHashSet(StringComparer.Ordinal);
        var familyIds = families.Select(x => x.ExternalId).ToHashSet(StringComparer.Ordinal);
        foreach (var family in families)
        {
            foreach (var personId in new[] { family.HusbandId, family.WifeId }.Concat(family.ChildIds))
                if (personId is not null && !individualIds.Contains(personId))
                    diagnostics.Add($"Family {family.ExternalId} references a missing individual.");
        }
        foreach (var person in individuals)
        {
            foreach (var familyId in person.FamilyAsChildIds.Concat(person.FamilyAsSpouseIds))
                if (!familyIds.Contains(familyId))
                    diagnostics.Add($"Individual {person.ExternalId} references a missing family.");
        }

        return new GedcomDocument(version, characterEncoding, individuals, families,
            sourceCount, mediaCount, diagnostics.Distinct().ToArray());
    }

    private static GedcomIndividual ParseIndividual(GedcomRecord record, DateOnly evaluationDate)
    {
        var nameLine = record.Lines.FirstOrDefault(x => x.Level == 1 && x.Tag == "NAME");
        var given = ChildValue(record.Lines, nameLine, "GIVN");
        var surname = ChildValue(record.Lines, nameLine, "SURN");
        if (string.IsNullOrWhiteSpace(given) && string.IsNullOrWhiteSpace(surname))
            (given, surname) = ParseDisplayName(nameLine?.Value);

        var birth = ParseEvent(record.Lines, "BIRT");
        var death = ParseEvent(record.Lines, "DEAT");
        var hasDeath = death.MarkerPresent;
        var cutoffYear = evaluationDate.Year - 110;
        var privacy = hasDeath
            ? GedcomPrivacyClassification.ExplicitlyDeceased
            : birth.Date?.Year is int birthYear && birthYear <= cutoffYear
                ? GedcomPrivacyClassification.HistoricalByAge
                : GedcomPrivacyClassification.ProtectedUncertain;

        return new GedcomIndividual(
            record.Header.Xref!,
            given ?? string.Empty,
            surname ?? string.Empty,
            record.Lines.FirstOrDefault(x => x.Level == 1 && x.Tag == "SEX")?.Value ?? string.Empty,
            birth.Date,
            birth.Place,
            death.Date,
            death.Place,
            hasDeath,
            privacy,
            record.Lines.Where(x => x.Level == 1 && x.Tag == "FAMC").Select(x => x.Value).Where(x => x is not null).Cast<string>().ToArray(),
            record.Lines.Where(x => x.Level == 1 && x.Tag == "FAMS").Select(x => x.Value).Where(x => x is not null).Cast<string>().ToArray());
    }

    private static GedcomFamily ParseFamily(GedcomRecord record)
    {
        var marriage = ParseEvent(record.Lines, "MARR");
        return new GedcomFamily(
            record.Header.Xref!,
            record.Lines.FirstOrDefault(x => x.Level == 1 && x.Tag == "HUSB")?.Value,
            record.Lines.FirstOrDefault(x => x.Level == 1 && x.Tag == "WIFE")?.Value,
            record.Lines.Where(x => x.Level == 1 && x.Tag == "CHIL").Select(x => x.Value).Where(x => x is not null).Cast<string>().ToArray(),
            marriage.Date,
            marriage.Place);
    }

    private static (bool MarkerPresent, GedcomDate? Date, string? Place) ParseEvent(
        IReadOnlyList<GedcomLine> lines,
        string tag)
    {
        var markerIndex = -1;
        for (var index = 0; index < lines.Count; index++)
            if (lines[index].Level == 1 && lines[index].Tag == tag) { markerIndex = index; break; }
        if (markerIndex < 0) return (false, null, null);

        string? date = null;
        string? place = null;
        for (var index = markerIndex + 1; index < lines.Count && lines[index].Level > 1; index++)
        {
            if (lines[index].Level == 2 && lines[index].Tag == "DATE") date = lines[index].Value;
            if (lines[index].Level == 2 && lines[index].Tag == "PLAC") place = lines[index].Value;
        }
        return (true, ParseDate(date), place);
    }

    private static GedcomDate? ParseDate(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var normalized = text.Trim();
        var tokens = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var yearToken = tokens.LastOrDefault(x => x.Length == 4 && int.TryParse(x, out _));
        var year = int.TryParse(yearToken, out var parsedYear) ? parsedYear : (int?)null;
        var exactText = string.Join(' ', tokens.SkipWhile(x => x is "ABT" or "AFT" or "BEF" or "CAL" or "EST"));
        DateOnly? exact = null;
        foreach (var format in new[] { "d MMM yyyy", "MMM yyyy", "yyyy" })
            if (DateOnly.TryParseExact(exactText, format, CultureInfo.InvariantCulture,
                    DateTimeStyles.AllowWhiteSpaces, out var parsed)) { exact = parsed; break; }
        return new GedcomDate(normalized, exact, year);
    }

    private static (string? Given, string? Surname) ParseDisplayName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return (null, null);
        var firstSlash = value.IndexOf('/');
        var secondSlash = firstSlash < 0 ? -1 : value.IndexOf('/', firstSlash + 1);
        if (firstSlash < 0 || secondSlash < 0) return (value.Trim(), null);
        return (value[..firstSlash].Trim(), value[(firstSlash + 1)..secondSlash].Trim());
    }

    private static string? ChildValue(IReadOnlyList<GedcomLine> lines, GedcomLine? parent, string childTag)
    {
        if (parent is null) return null;
        var index = lines.IndexOf(parent);
        for (var child = index + 1; child < lines.Count && lines[child].Level > parent.Level; child++)
            if (lines[child].Level == parent.Level + 1 && lines[child].Tag == childTag)
                return lines[child].Value;
        return null;
    }

    private static string? FindHeaderValue(IReadOnlyList<GedcomLine> lines, string parentTag, string childTag)
    {
        var parent = lines.TakeWhile(x => x.Level != 0 || x.Tag == "HEAD")
            .FirstOrDefault(x => x.Level == 1 && x.Tag == parentTag);
        return ChildValue(lines, parent, childTag);
    }

    private static IReadOnlyList<GedcomRecord> SplitRecords(IReadOnlyList<GedcomLine> lines)
    {
        var records = new List<GedcomRecord>();
        for (var index = 0; index < lines.Count;)
        {
            if (lines[index].Level != 0) { index++; continue; }
            var end = index + 1;
            while (end < lines.Count && lines[end].Level != 0) end++;
            records.Add(new GedcomRecord(lines[index], lines.Skip(index + 1).Take(end - index - 1).ToArray()));
            index = end;
        }
        return records;
    }

    private static bool TryParseLine(string text, out GedcomLine line)
    {
        line = default!;
        var firstSpace = text.IndexOf(' ');
        if (firstSpace <= 0 || !int.TryParse(text[..firstSpace], out var level)) return false;
        var remainder = text[(firstSpace + 1)..];
        string? xref = null;
        if (remainder.StartsWith('@'))
        {
            var xrefEnd = remainder.IndexOf('@', 1);
            if (xrefEnd < 1) return false;
            xref = remainder[..(xrefEnd + 1)];
            remainder = remainder[(xrefEnd + 1)..].TrimStart();
        }
        var tagEnd = remainder.IndexOf(' ');
        var tag = tagEnd < 0 ? remainder : remainder[..tagEnd];
        var value = tagEnd < 0 ? null : remainder[(tagEnd + 1)..];
        line = new GedcomLine(level, xref, tag, value);
        return true;
    }

    internal sealed record GedcomLine(int Level, string? Xref, string Tag, string? Value);
    private sealed record GedcomRecord(GedcomLine Header, IReadOnlyList<GedcomLine> Lines);
}

internal static class ReadOnlyListExtensions
{
    public static int IndexOf<T>(this IReadOnlyList<T> items, T item)
    {
        for (var index = 0; index < items.Count; index++)
            if (EqualityComparer<T>.Default.Equals(items[index], item)) return index;
        return -1;
    }
}
