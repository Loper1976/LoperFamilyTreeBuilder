using LoperFamilyTreeBuilder.Core.Entities;
using LoperFamilyTreeBuilder.Core.Policies;

namespace LoperFamilyTreeBuilder.Tests;

public sealed class LoperIdTests
{
    [Fact]
    public void LoperIdUsesStableSequentialFormat()
    {
        Assert.Equal("LOPER-000001", LoperIdPolicy.Format(1));
        Assert.Equal("LOPER-123456", LoperIdPolicy.Format(123456));
    }

    [Fact]
    public void LoperIdIsProtectedAndCannotBeReassigned()
    {
        var person = new Person("Fictional", "Researcher");
        var identifier = person.AddLoperId("LOPER-000001");

        Assert.True(identifier.IsProtected);
        Assert.Throws<InvalidOperationException>(() =>
            identifier.ChangeValue("LOPER-000002"));
        Assert.Throws<InvalidOperationException>(() =>
            person.AddLoperId("LOPER-000002"));
    }
}
