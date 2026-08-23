namespace LoperFamilyTreeBuilder.Core.Models;

public sealed record PersonListItem(
    Guid Id,
    string GivenName,
    string MiddleName,
    string Surname,
    string Suffix,
    DateOnly? BirthDate,
    DateOnly? DeathDate,
    bool IsLiving,
    string? LoperId,
    string? LegacyNumber,
    string? PrimaryBranchName)
{
    public string DisplayName
    {
        get
        {
            var values = new[]
            {
                GivenName,
                MiddleName,
                Surname,
                Suffix
            }
            .Where(value => !string.IsNullOrWhiteSpace(value));

            return string.Join(" ", values);
        }
    }
}
