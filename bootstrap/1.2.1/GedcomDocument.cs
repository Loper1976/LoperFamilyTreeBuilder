namespace LoperFamilyTreeBuilder.ImportExport.Gedcom;

public sealed class GedcomDocument
{
    public List<GedcomIndividual> Individuals { get; } = [];
    public List<GedcomFamily> Families { get; } = [];
    public List<GedcomSource> Sources { get; } = [];
    public List<GedcomUnsupportedTag> UnsupportedTags { get; } = [];
    public List<string> Warnings { get; } = [];
    public List<string> Errors { get; } = [];
    public int LineCount { get; set; }
}

public sealed class GedcomIndividual
{
    public string Pointer { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string GivenName { get; set; } = string.Empty;
    public string Surname { get; set; } = string.Empty;
    public string Sex { get; set; } = string.Empty;
    public string BirthDateText { get; set; } = string.Empty;
    public DateOnly? BirthDate { get; set; }
    public string BirthPlace { get; set; } = string.Empty;
    public string DeathDateText { get; set; } = string.Empty;
    public DateOnly? DeathDate { get; set; }
    public string DeathPlace { get; set; } = string.Empty;
    public string LegacyNumber { get; set; } = string.Empty;
    public List<string> Notes { get; } = [];
    public List<string> SourcePointers { get; } = [];
    public List<string> FamilyChildPointers { get; } = [];
    public List<string> FamilySpousePointers { get; } = [];
}

public sealed class GedcomFamily
{
    public string Pointer { get; set; } = string.Empty;
    public string? HusbandPointer { get; set; }
    public string? WifePointer { get; set; }
    public List<string> ChildPointers { get; } = [];
    public string MarriageDateText { get; set; } = string.Empty;
    public DateOnly? MarriageDate { get; set; }
    public string MarriagePlace { get; set; } = string.Empty;
    public List<string> Notes { get; } = [];
}

public sealed class GedcomSource
{
    public string Pointer { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string PublicationFacts { get; set; } = string.Empty;
    public List<string> Notes { get; } = [];
}

public sealed record GedcomUnsupportedTag(
    int LineNumber,
    string Tag,
    string Value,
    string RecordPointer = "");
