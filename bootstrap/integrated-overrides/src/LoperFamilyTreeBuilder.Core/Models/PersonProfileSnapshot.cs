namespace LoperFamilyTreeBuilder.Core.Models;

public sealed record PersonProfileSnapshot(
    Guid Id,
    string GivenName,
    string MiddleName,
    string Surname,
    string Suffix,
    DateOnly? BirthDate,
    DateOnly? DeathDate,
    bool IsLiving,
    string? LegacyNumber,
    IReadOnlyList<string> FamilyBranches,
    IReadOnlyList<PersonProfileRelative> Parents,
    IReadOnlyList<PersonProfileRelative> Children,
    DateTimeOffset CreatedUtc,
    DateTimeOffset ModifiedUtc)
{
    public string DisplayName => string.Join(
        " ",
        new[] { GivenName, MiddleName, Surname, Suffix }
            .Where(value => !string.IsNullOrWhiteSpace(value)));

    public string Lifespan
    {
        get
        {
            var birth = BirthDate?.Year.ToString() ?? "?";
            var death = IsLiving ? "Living" : DeathDate?.Year.ToString() ?? "?";
            return $"{birth} - {death}";
        }
    }

    public bool HasProtectedLegacyNumber => !string.IsNullOrWhiteSpace(LegacyNumber);
}
