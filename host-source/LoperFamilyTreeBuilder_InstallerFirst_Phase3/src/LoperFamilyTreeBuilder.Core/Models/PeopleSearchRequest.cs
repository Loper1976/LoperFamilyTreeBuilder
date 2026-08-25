namespace LoperFamilyTreeBuilder.Core.Models;

public sealed class PeopleSearchRequest
{
    public string SearchText { get; set; } = string.Empty;

    public string Surname { get; set; } = string.Empty;

    public string LegacyNumberPrefix { get; set; } = string.Empty;

    public Guid? FamilyBranchId { get; set; }

    public LivingStatusFilter LivingStatus { get; set; } =
        LivingStatusFilter.All;

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 50;

    public string SortBy { get; set; } = "surname";

    public bool Descending { get; set; }
}
