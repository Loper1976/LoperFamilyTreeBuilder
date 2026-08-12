namespace LoperFamilyTreeBuilder.Core.Models;

public sealed record FamilyTreePersonSnapshot(
    Guid Id,
    string DisplayName,
    DateOnly? BirthDate,
    DateOnly? DeathDate,
    bool IsLiving,
    string? LegacyNumber);
