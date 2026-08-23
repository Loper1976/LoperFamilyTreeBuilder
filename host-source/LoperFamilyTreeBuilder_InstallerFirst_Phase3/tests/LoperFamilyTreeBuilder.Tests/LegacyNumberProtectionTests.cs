using LoperFamilyTreeBuilder.Core.Entities;
using LoperFamilyTreeBuilder.Core.Policies;

namespace LoperFamilyTreeBuilder.Tests;

public sealed class LegacyNumberProtectionTests
{
    [Fact]
    public void LegacyNumber_PreservesExactHistoricalText()
    {
        const string original = "21313.00";

        var preserved = LegacyNumberPolicy.PreserveExact(original);

        Assert.Equal(original, preserved);
    }

    [Fact]
    public void LegacyNumber_DoesNotTrimOrNormalize()
    {
        const string original = " 21313.00 ";

        var preserved = LegacyNumberPolicy.PreserveExact(original);

        Assert.Equal(" 21313.00 ", preserved);
    }

    [Fact]
    public void ProtectedLegacyNumber_CannotBeChanged()
    {
        var person = new Person("Robert", "Loper");
        var legacyNumber = person.AddLegacyNumber("21313.00");

        var exception = Assert.Throws<InvalidOperationException>(
            () => legacyNumber.ChangeValue("21314.00"));

        Assert.Contains("protected", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("21313.00", legacyNumber.Value);
    }

    [Fact]
    public void Person_CannotReceiveSecondLegacyNumber()
    {
        var person = new Person("Robert", "Loper");
        person.AddLegacyNumber("21313.00");

        Assert.Throws<InvalidOperationException>(
            () => person.AddLegacyNumber("99999.00"));
    }

    [Fact]
    public void ExactComparison_IsOrdinal()
    {
        Assert.True(
            LegacyNumberPolicy.IsExactMatch("21313.00", "21313.00"));

        Assert.False(
            LegacyNumberPolicy.IsExactMatch("21313.00", "21313.0"));
    }
}
