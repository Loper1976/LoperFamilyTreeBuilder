using LoperFamilyTreeBuilder.Core.Entities;
using LoperFamilyTreeBuilder.ImportExport.Gedcom;

namespace LoperFamilyTreeBuilder.Tests;

public sealed class GedcomImportReadinessTests
{
    [Fact]
    public void Parser_preserves_legacy_number_notes_places_and_source_references()
    {
        const string gedcom = """
0 HEAD
0 @S1@ SOUR
1 TITL 1940 United States Federal Census
0 @I1@ INDI
1 NAME Robert /Loper/
1 _LEGACY 21313.00
1 BIRT
2 DATE 30 JUL 1927
2 PLAC Houston, Texas
1 NOTE First note line
2 CONT second line
1 SOUR @S1@
0 TRLR
""";
        var doc = new GedcomParser().Parse(gedcom);
        Assert.Empty(doc.Errors);
        var person = Assert.Single(doc.Individuals);
        Assert.Equal("21313.00", person.LegacyNumber);
        Assert.Equal("Houston, Texas", person.BirthPlace);
        Assert.Contains("second line", Assert.Single(person.Notes));
        Assert.Equal("@S1@", Assert.Single(person.SourcePointers));
        Assert.Single(doc.Sources);
    }

    [Fact]
    public void Protected_legacy_number_cannot_be_replaced_by_import_logic()
    {
        var person = new Person("Robert", "Loper");
        person.AddLegacyNumber("21313.00");
        Assert.Throws<InvalidOperationException>(() => person.AddLegacyNumber("21314.00"));
        Assert.Equal("21313.00", person.Identifiers.Single(x => x.IdentifierType == PersonIdentifierType.LegacyNumber).Value);
    }

    [Fact]
    public void Applied_import_can_transition_to_rolled_back_status()
    {
        var session = new GedcomImportSession("test.ged", "Imports/test.ged", new string('a', 64), 1, 0, 0, "{}");
        session.Approve();
        session.MarkApplied("backup.bak");
        session.MarkRolledBack("Test rollback");
        Assert.Equal(GedcomImportStatus.RolledBack, session.Status);
    }
}
