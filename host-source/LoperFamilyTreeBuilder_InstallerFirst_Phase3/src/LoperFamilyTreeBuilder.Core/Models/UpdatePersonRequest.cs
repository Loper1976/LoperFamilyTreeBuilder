namespace LoperFamilyTreeBuilder.Core.Models;

/// <summary>
/// Normal person editing intentionally excludes Robert J. Loper Legacy Number.
/// Protected historical identifiers are managed by preservation/import workflows only.
/// </summary>
public sealed class UpdatePersonRequest
{
    public string GivenName { get; set; } = string.Empty;

    public string MiddleName { get; set; } = string.Empty;

    public string Surname { get; set; } = string.Empty;

    public string Suffix { get; set; } = string.Empty;

    public DateOnly? BirthDate { get; set; }

    public DateOnly? DeathDate { get; set; }

    public bool IsLiving { get; set; } = true;

    public Guid? PrimaryFamilyBranchId { get; set; }
}
