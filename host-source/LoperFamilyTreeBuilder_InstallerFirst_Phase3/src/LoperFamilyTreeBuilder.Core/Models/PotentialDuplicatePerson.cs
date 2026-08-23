namespace LoperFamilyTreeBuilder.Core.Models;

public sealed record PotentialDuplicatePerson(
    Guid Id,
    string DisplayName,
    DateOnly? BirthDate,
    DateOnly? DeathDate,
    string? LegacyNumber,
    string Reason);
