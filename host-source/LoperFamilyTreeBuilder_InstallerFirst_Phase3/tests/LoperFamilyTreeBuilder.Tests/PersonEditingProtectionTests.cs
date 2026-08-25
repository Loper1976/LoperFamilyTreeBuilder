using LoperFamilyTreeBuilder.Core.Entities;
using LoperFamilyTreeBuilder.Core.Models;

namespace LoperFamilyTreeBuilder.Tests;

public sealed class PersonEditingProtectionTests
{
    [Fact]
    public void NormalEditRequest_DoesNotExposeLegacyNumber()
    {
        var legacyProperty = typeof(UpdatePersonRequest)
            .GetProperty("LegacyNumber");

        Assert.Null(legacyProperty);
    }

    [Fact]
    public void NormalPersonEdits_DoNotMutateProtectedLegacyIdentifier()
    {
        var person = new Person("Robert", "Loper");
        var identifier = person.AddLegacyNumber("21313.00");

        person.UpdateName("Robert", "Joel", "Loper", string.Empty);
        person.SetBirthDate(new DateOnly(1927, 7, 30));
        person.SetLivingStatus(true);

        Assert.Equal("21313.00", identifier.Value);
        Assert.True(identifier.IsProtected);
    }
}
