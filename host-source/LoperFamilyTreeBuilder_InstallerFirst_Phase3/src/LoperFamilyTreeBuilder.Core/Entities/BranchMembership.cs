namespace LoperFamilyTreeBuilder.Core.Entities;

public sealed class BranchMembership
{
    private BranchMembership()
    {
    }

    public BranchMembership(Guid personId, Guid familyBranchId, bool isPrimary)
    {
        Id = Guid.NewGuid();
        PersonId = personId;
        FamilyBranchId = familyBranchId;
        IsPrimary = isPrimary;
        CreatedUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }

    public Guid PersonId { get; private set; }

    public Guid FamilyBranchId { get; private set; }

    public bool IsPrimary { get; private set; }

    public DateTimeOffset CreatedUtc { get; private set; }

    public Person Person { get; private set; } = null!;

    public FamilyBranch FamilyBranch { get; private set; } = null!;

    public void SetPrimary(bool isPrimary)
    {
        IsPrimary = isPrimary;
    }
}
