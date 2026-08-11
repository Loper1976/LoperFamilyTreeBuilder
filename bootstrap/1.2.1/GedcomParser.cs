using System.Globalization;
using System.Text;

namespace LoperFamilyTreeBuilder.ImportExport.Gedcom;

public sealed class GedcomParser
{
    private static readonly HashSet<string> SupportedTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "HEAD", "TRLR", "CHAR", "GEDC", "VERS", "FORM", "SOUR", "SUBM",
        "INDI", "FAM", "NAME", "GIVN", "SURN", "SEX", "BIRT", "DEAT", "DATE", "PLAC",
        "FAMC", "FAMS", "HUSB", "WIFE", "CHIL", "MARR", "TITL", "AUTH", "PUBL",
        "NOTE", "CONT", "CONC", "PAGE", "DATA", "TEXT", "OBJE", "FILE", "RIN", "CHAN"
    };

    public GedcomDocument Parse(byte[] content)
    {
        ArgumentNullException.ThrowIfNull(content);
        return Parse(Decode(content));
    }

    public GedcomDocument Parse(string text)
    {
        var document = new GedcomDocument();
        if (string.IsNullOrWhiteSpace(text))
        {
            document.Errors.Add("The GEDCOM file is empty.");
            return document;
        }

        GedcomIndividual? currentIndividual = null;
        GedcomFamily? currentFamily = null;
        GedcomSource? currentSource = null;
        string context = string.Empty;
        string currentRecordPointer = string.Empty;
        string noteContext = string.Empty;

        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        document.LineCount = lines.Length;

        for (var index = 0; index < lines.Length; index++)
        {
            var raw = lines[index];
            if (string.IsNullOrWhiteSpace(raw)) continue;

            if (!TryParseLine(raw, out var level, out var pointer, out var tag, out var value))
            {
                document.Errors.Add($"Line {index + 1}: invalid GEDCOM line format.");
                continue;
            }

            if (level == 0)
            {
                currentIndividual = null;
                currentFamily = null;
                currentSource = null;
                context = string.Empty;
                noteContext = string.Empty;
                currentRecordPointer = pointer;

                if (tag.Equals("INDI", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(pointer))
                {
                    currentIndividual = new GedcomIndividual { Pointer = pointer };
                    document.Individuals.Add(currentIndividual);
                }
                else if (tag.Equals("FAM", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(pointer))
                {
                    currentFamily = new GedcomFamily { Pointer = pointer };
                    document.Families.Add(currentFamily);
                }
                else if (tag.Equals("SOUR", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(pointer))
                {
                    currentSource = new GedcomSource { Pointer = pointer };
                    document.Sources.Add(currentSource);
                }
                continue;
            }

            var isCustom = tag.StartsWith("_", StringComparison.Ordinal);
            if (!SupportedTags.Contains(tag) || isCustom)
                document.UnsupportedTags.Add(new GedcomUnsupportedTag(index + 1, tag, value, currentRecordPointer));

            if (currentIndividual is not null)
            {
                if (IsLegacyNumberTag(tag))
                {
                    if (string.IsNullOrEmpty(currentIndividual.LegacyNumber))
                        currentIndividual.LegacyNumber = value;
                    else if (!string.Equals(currentIndividual.LegacyNumber, value, StringComparison.Ordinal))
                        document.Warnings.Add($"Line {index + 1}: multiple Legacy Number values were found for {currentIndividual.Pointer}; manual review is required.");
                    continue;
                }

                switch (tag.ToUpperInvariant())
                {
                    case "NAME":
                        currentIndividual.Name = value;
                        ParseName(value, currentIndividual);
                        context = string.Empty;
                        break;
                    case "GIVN": currentIndividual.GivenName = value.Trim(); break;
                    case "SURN": currentIndividual.Surname = value.Trim(); break;
                    case "SEX": currentIndividual.Sex = value.Trim(); break;
                    case "BIRT": context = "BIRT"; noteContext = string.Empty; break;
                    case "DEAT": context = "DEAT"; noteContext = string.Empty; break;
                    case "DATE" when context == "BIRT":
                        currentIndividual.BirthDateText = value;
                        currentIndividual.BirthDate = ParseExactDate(value, document.Warnings, index + 1);
                        break;
                    case "DATE" when context == "DEAT":
                        currentIndividual.DeathDateText = value;
                        currentIndividual.DeathDate = ParseExactDate(value, document.Warnings, index + 1);
                        break;
                    case "PLAC" when context == "BIRT": currentIndividual.BirthPlace = value; break;
                    case "PLAC" when context == "DEAT": currentIndividual.DeathPlace = value; break;
                    case "SOUR":
                        if (!string.IsNullOrWhiteSpace(value)) currentIndividual.SourcePointers.Add(value.Trim());
                        context = string.Empty;
                        break;
                    case "NOTE":
                        currentIndividual.Notes.Add(value);
                        noteContext = "INDI";
                        break;
                    case "CONT" when noteContext == "INDI": AppendNote(currentIndividual.Notes, value, true); break;
                    case "CONC" when noteContext == "INDI": AppendNote(currentIndividual.Notes, value, false); break;
                    case "FAMC": currentIndividual.FamilyChildPointers.Add(value.Trim()); break;
                    case "FAMS": currentIndividual.FamilySpousePointers.Add(value.Trim()); break;
                }
            }
            else if (currentFamily is not null)
            {
                switch (tag.ToUpperInvariant())
                {
                    case "HUSB": currentFamily.HusbandPointer = value.Trim(); break;
                    case "WIFE": currentFamily.WifePointer = value.Trim(); break;
                    case "CHIL": currentFamily.ChildPointers.Add(value.Trim()); break;
                    case "MARR": context = "MARR"; noteContext = string.Empty; break;
                    case "DATE" when context == "MARR":
                        currentFamily.MarriageDateText = value;
                        currentFamily.MarriageDate = ParseExactDate(value, document.Warnings, index + 1);
                        break;
                    case "PLAC" when context == "MARR": currentFamily.MarriagePlace = value; break;
                    case "NOTE": currentFamily.Notes.Add(value); noteContext = "FAM"; break;
                    case "CONT" when noteContext == "FAM": AppendNote(currentFamily.Notes, value, true); break;
                    case "CONC" when noteContext == "FAM": AppendNote(currentFamily.Notes, value, false); break;
                }
            }
            else if (currentSource is not null)
            {
                switch (tag.ToUpperInvariant())
                {
                    case "TITL": currentSource.Title = value.Trim(); break;
                    case "AUTH": currentSource.Author = value.Trim(); break;
                    case "PUBL": currentSource.PublicationFacts = value; break;
                    case "NOTE": currentSource.Notes.Add(value); noteContext = "SOUR"; break;
                    case "CONT" when noteContext == "SOUR": AppendNote(currentSource.Notes, value, true); break;
                    case "CONC" when noteContext == "SOUR": AppendNote(currentSource.Notes, value, false); break;
                }
            }
        }

        if (document.Individuals.Count == 0 && document.Families.Count == 0)
            document.Warnings.Add("No INDI or FAM records were found.");

        foreach (var pointer in document.Individuals.GroupBy(i => i.Pointer, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1).Select(g => g.Key))
            document.Errors.Add($"Duplicate individual pointer {pointer}.");
        foreach (var pointer in document.Families.GroupBy(i => i.Pointer, StringComparer.OrdinalIgnoreCase).Where(g => g.Count() > 1).Select(g => g.Key))
            document.Errors.Add($"Duplicate family pointer {pointer}.");

        return document;
    }

    private static bool IsLegacyNumberTag(string tag) =>
        tag.Contains("LEGACY", StringComparison.OrdinalIgnoreCase)
        || tag.Equals("_LNUM", StringComparison.OrdinalIgnoreCase)
        || tag.Equals("_LOPER", StringComparison.OrdinalIgnoreCase)
        || tag.Equals("_LOPERNO", StringComparison.OrdinalIgnoreCase);

    private static void AppendNote(List<string> notes, string value, bool newLine)
    {
        if (notes.Count == 0) { notes.Add(value); return; }
        var last = notes[^1];
        notes[^1] = newLine ? last + Environment.NewLine + value : last + value;
    }

    private static bool TryParseLine(string raw, out int level, out string pointer, out string tag, out string value)
    {
        level = 0; pointer = string.Empty; tag = string.Empty; value = string.Empty;
        var firstSpace = raw.IndexOf(' ');
        if (firstSpace <= 0 || !int.TryParse(raw[..firstSpace], out level)) return false;
        var remainder = raw[(firstSpace + 1)..].Trim();
        if (remainder.Length == 0) return false;
        var parts = remainder.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return false;
        var tagIndex = 0;
        if (parts[0].StartsWith('@') && parts[0].EndsWith('@')) { pointer = parts[0]; tagIndex = 1; }
        if (tagIndex >= parts.Length) return false;
        tag = parts[tagIndex];
        var tagPosition = remainder.IndexOf(tag, StringComparison.Ordinal);
        var valueStart = tagPosition + tag.Length;
        value = valueStart < remainder.Length ? remainder[valueStart..].Trim() : string.Empty;
        return true;
    }

    private static void ParseName(string name, GedcomIndividual individual)
    {
        var firstSlash = name.IndexOf('/');
        var secondSlash = firstSlash >= 0 ? name.IndexOf('/', firstSlash + 1) : -1;
        if (firstSlash >= 0 && secondSlash > firstSlash)
        {
            individual.GivenName = name[..firstSlash].Trim();
            individual.Surname = name[(firstSlash + 1)..secondSlash].Trim();
        }
        else individual.GivenName = name.Trim();
    }

    private static DateOnly? ParseExactDate(string value, List<string> warnings, int lineNumber)
    {
        var normalized = value.Trim();
        var formats = new[] { "d MMM yyyy", "dd MMM yyyy" };
        foreach (var format in formats)
        {
            if (DateOnly.TryParseExact(normalized, format, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var date))
                return date;
        }
        if (!string.IsNullOrWhiteSpace(normalized))
            warnings.Add($"Line {lineNumber}: date '{normalized}' is not an exact day-month-year value and will remain review-only.");
        return null;
    }

    private static string Decode(byte[] content)
    {
        if (content.Length >= 3 && content[0] == 0xEF && content[1] == 0xBB && content[2] == 0xBF)
            return Encoding.UTF8.GetString(content, 3, content.Length - 3);
        return Encoding.UTF8.GetString(content);
    }
}
