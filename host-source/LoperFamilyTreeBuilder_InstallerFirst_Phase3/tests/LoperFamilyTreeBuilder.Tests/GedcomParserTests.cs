using System.Text;
using LoperFamilyTreeBuilder.ImportExport.Gedcom;

namespace LoperFamilyTreeBuilder.Tests;

public sealed class GedcomParserTests
{
    [Fact]
    public async Task ParsesPeopleFamiliesAndPrivacyWithoutPromotingAnything()
    {
        const string fixture = """
            0 HEAD
            1 GEDC
            2 VERS 5.5.1
            1 CHAR UTF-8
            0 @I1@ INDI
            1 NAME Historical /Example/
            2 GIVN Historical
            2 SURN Example
            1 SEX M
            1 BIRT
            2 DATE 12 MAR 1900
            2 PLAC Example County
            0 @I2@ INDI
            1 NAME Deceased /Example/
            1 DEAT Y
            2 DATE ABT 1980
            0 @I3@ INDI
            1 NAME Protected /Example/
            1 BIRT
            2 DATE 1990
            0 @F1@ FAM
            1 HUSB @I1@
            1 WIFE @I2@
            1 CHIL @I3@
            1 MARR
            2 DATE 1920
            2 PLAC Example City
            0 @S1@ SOUR
            1 TITL Fictional source
            0 TRLR
            """;
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(fixture));

        var document = await new GedcomParser().ParseAsync(stream, new DateOnly(2026, 8, 23));

        Assert.True(document.IsStructurallyValid);
        Assert.Equal("5.5.1", document.Version);
        Assert.Equal("UTF-8", document.CharacterEncoding);
        Assert.Equal(3, document.Individuals.Count);
        Assert.Single(document.Families);
        Assert.Equal(1, document.SourceRecordCount);
        Assert.Equal(2, document.ResearchEligibleCount);
        Assert.Equal(GedcomPrivacyClassification.HistoricalByAge, document.Individuals[0].Privacy);
        Assert.Equal(GedcomPrivacyClassification.ExplicitlyDeceased, document.Individuals[1].Privacy);
        Assert.Equal(GedcomPrivacyClassification.ProtectedUncertain, document.Individuals[2].Privacy);
        Assert.Equal("@I3@", document.Families[0].ChildIds.Single());
    }

    [Fact]
    public async Task ReportsBrokenReferencesAndMissingTrailer()
    {
        const string fixture = """
            0 HEAD
            1 GEDC
            2 VERS 5.5.1
            1 CHAR UTF-8
            0 @F1@ FAM
            1 CHIL @MISSING@
            """;
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes(fixture));

        var document = await new GedcomParser().ParseAsync(stream);

        Assert.False(document.IsStructurallyValid);
        Assert.Contains(document.Diagnostics, x => x.Contains("trailer", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(document.Diagnostics, x => x.Contains("missing individual", StringComparison.OrdinalIgnoreCase));
    }
}
