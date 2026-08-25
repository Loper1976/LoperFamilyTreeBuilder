namespace LoperFamilyTreeBuilder.Core.Models;

public sealed record PersonProfileModel(
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
    DateTimeOffset CreatedUtc,
    DateTimeOffset ModifiedUtc,
    IReadOnlyList<PersonBranchProfileItem> Branches,
    IReadOnlyList<PersonRelationshipProfileItem> Parents,
    IReadOnlyList<PersonRelationshipProfileItem> Children,
    IReadOnlyList<PersonAuditProfileItem> AuditHistory)
{
    public string DisplayName
    {
        get
        {
            var values = new[] { GivenName, MiddleName, Surname, Suffix }
                .Where(value => !string.IsNullOrWhiteSpace(value));

            var name = string.Join(" ", values);
            return string.IsNullOrWhiteSpace(name) ? "(Unnamed person)" : name;
        }
    }

    public Guid? PrimaryBranchId =>
        Branches.FirstOrDefault(branch => branch.IsPrimary)?.BranchId;
}

public sealed record PersonBranchProfileItem(
    Guid BranchId,
    string Name,
    string ShortCode,
    bool IsPrimary);

public sealed record PersonRelationshipProfileItem(
    Guid PersonId,
    string DisplayName,
    string RelationshipType,
    string? LegacyNumber);

public sealed record PersonAuditProfileItem(
    Guid Id,
    DateTimeOffset OccurredUtc,
    string Action,
    string Actor,
    string Summary,
    string? PreviousValueJson,
    string? NewValueJson);
