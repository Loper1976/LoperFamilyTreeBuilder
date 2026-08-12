using LoperFamilyTreeBuilder.Core.Models;

namespace LoperFamilyTreeBuilder.Tests;

public sealed class PersonProfileSnapshotTests
{
    [Fact]
    public void Legacy_number_is_preserved_exactly_as_supplied()
    {
        const string legacyNumber = "21314.00";
        var profile = CreateProfile(legacyNumber);

        Assert.Equal(legacyNumber, profile.LegacyNumber);
        Assert.True(profile.HasProtectedLegacyNumber);
    }

    [Fact]
    public void Display_name_uses_recorded_name_parts_without_identifier_rewriting()
    {
        var profile = CreateProfile("21314.00");

        Assert.Equal("Philip Argus Loper", profile.DisplayName);
    }

    [Fact]
    public void Lifespan_marks_living_people_without_fabricating_a_death_year()
    {
        var profile = CreateProfile("21314.00");

        Assert.Equal("1976 - Living", profile.Lifespan);
    }

    [Fact]
    public void Relationship_collections_are_exposed_without_changing_legacy_numbers()
    {
        var parent = new PersonProfileRelative(Guid.NewGuid(), "Robert J. Loper", "21313.00", "Biological");
        var profile = CreateProfile("21314.00", parents: [parent]);

        Assert.Single(profile.Parents);
        Assert.Equal("21313.00", profile.Parents[0].LegacyNumber);
        Assert.Equal("21314.00", profile.LegacyNumber);
    }

    private static PersonProfileSnapshot CreateProfile(
        string? legacyNumber,
        IReadOnlyList<PersonProfileRelative>? parents = null)
    {
        return new PersonProfileSnapshot(
            Guid.NewGuid(),
            "Philip",
            "Argus",
            "Loper",
            string.Empty,
            new DateOnly(1976, 6, 1),
            null,
            true,
            legacyNumber,
            ["Loper"],
            parents ?? [],
            [],
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow);
    }
}
