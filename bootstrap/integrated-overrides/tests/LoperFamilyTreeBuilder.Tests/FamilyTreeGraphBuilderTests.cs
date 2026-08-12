using LoperFamilyTreeBuilder.Core.Entities;
using LoperFamilyTreeBuilder.Core.Genealogy;
using LoperFamilyTreeBuilder.Core.Models;

namespace LoperFamilyTreeBuilder.Tests;

public sealed class FamilyTreeGraphBuilderTests
{
    [Fact]
    public void DescendantViewBuildsGenerationsWithoutChangingLegacyNumbers()
    {
        var rootId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var grandchildId = Guid.NewGuid();
        const string exactLegacy = " 21313.00 ";
        var people = new[] { Person(rootId, "Robert J. Loper", exactLegacy), Person(childId, "Child Loper", "213131.00"), Person(grandchildId, "Grandchild Loper", "2131311.00") };
        var relationships = new[] {
            new FamilyTreeRelationshipSnapshot(rootId, childId, ParentChildRelationshipType.Biological),
            new FamilyTreeRelationshipSnapshot(childId, grandchildId, ParentChildRelationshipType.Biological)
        };
        var view = new FamilyTreeGraphBuilder().Build(rootId, FamilyTreeDirection.Descendants, 4, people, relationships);
        Assert.Equal(exactLegacy, view.Root.Person.LegacyNumber);
        Assert.Single(view.Root.Branches);
        Assert.Single(view.Root.Branches[0].Branches);
        Assert.Equal(2, view.Root.Branches[0].Branches[0].Generation);
        Assert.Equal(3, view.NodeCount);
    }

    [Fact]
    public void AncestorViewWalksFromChildToParents()
    {
        var parentId = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var people = new[] { Person(parentId, "Parent", ".00"), Person(childId, "Child", "1.00") };
        var relationships = new[] { new FamilyTreeRelationshipSnapshot(parentId, childId, ParentChildRelationshipType.Biological) };
        var view = new FamilyTreeGraphBuilder().Build(childId, FamilyTreeDirection.Ancestors, 3, people, relationships);
        Assert.Single(view.Root.Branches);
        Assert.Equal(parentId, view.Root.Branches[0].Person.Id);
    }

    [Fact]
    public void CycleIsSuppressedInsteadOfRecursingForever()
    {
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        var people = new[] { Person(first, "First", "1.00"), Person(second, "Second", "2.00") };
        var relationships = new[] {
            new FamilyTreeRelationshipSnapshot(first, second, ParentChildRelationshipType.Custom),
            new FamilyTreeRelationshipSnapshot(second, first, ParentChildRelationshipType.Custom)
        };
        var view = new FamilyTreeGraphBuilder().Build(first, FamilyTreeDirection.Descendants, 6, people, relationships);
        Assert.True(view.Root.Branches[0].Branches[0].CycleSuppressed);
    }

    [Fact]
    public void MissingRootIsRejected()
    {
        var builder = new FamilyTreeGraphBuilder();
        Assert.Throws<InvalidOperationException>(() => builder.Build(Guid.NewGuid(), FamilyTreeDirection.Descendants, 4, Array.Empty<FamilyTreePersonSnapshot>(), Array.Empty<FamilyTreeRelationshipSnapshot>()));
    }

    private static FamilyTreePersonSnapshot Person(Guid id, string name, string legacyNumber) => new(id, name, null, null, false, legacyNumber);
}
