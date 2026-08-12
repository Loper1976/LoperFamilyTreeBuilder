namespace LoperFamilyTreeBuilder.Core.Models;

public sealed record FamilyTreeView(
    FamilyTreeDirection Direction,
    int RequestedDepth,
    int NodeCount,
    bool NodeLimitReached,
    FamilyTreeNode Root);
