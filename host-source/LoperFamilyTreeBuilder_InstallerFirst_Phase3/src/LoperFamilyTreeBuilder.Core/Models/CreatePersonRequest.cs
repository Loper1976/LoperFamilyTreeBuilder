namespace LoperFamilyTreeBuilder.Core.Models;

public sealed class CreatePersonRequest
{
    public string GivenName { get; set; } = string.Empty;

    public string MiddleName { get; set; } = string.Empty;

    public string Surname { get; set; } = string.Empty;

    public string Suffix { get; set; } = string.Empty;

    public DateOnly? BirthDate { get; set; }

    public DateOnly? DeathDate { get; set; }

    public bool IsLiving { get; set; } = true;

    public string LegacyNumber { get; set; } = string.Empty;

    public Guid? FamilyBranchId { get; set; }
}
