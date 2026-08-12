namespace LoperFamilyTreeBuilder.Core.Models;

public sealed record PersonProfileRelative(
    Guid Id,
    string DisplayName,
    string? LegacyNumber,
    string RelationshipKind);
