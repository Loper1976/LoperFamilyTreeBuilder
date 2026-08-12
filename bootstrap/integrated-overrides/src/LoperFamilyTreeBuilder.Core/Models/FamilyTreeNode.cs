using LoperFamilyTreeBuilder.Core.Entities;

namespace LoperFamilyTreeBuilder.Core.Models;

public sealed record FamilyTreeNode(
    FamilyTreePersonSnapshot Person,
    ParentChildRelationshipType? RelationshipType,
    int Generation,
    bool CycleSuppressed,
    IReadOnlyList<FamilyTreeNode> Branches);
