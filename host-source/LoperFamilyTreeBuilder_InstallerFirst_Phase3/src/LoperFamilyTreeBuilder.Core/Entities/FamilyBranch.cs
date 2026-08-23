namespace LoperFamilyTreeBuilder.Core.Entities;

public sealed class FamilyBranch
{
    private readonly List<BranchMembership> _memberships = new();

    private FamilyBranch()
    {
    }

    public FamilyBranch(string name, string shortCode)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Branch name is required.", nameof(name));
        }

        Id = Guid.NewGuid();
        Name = name;
        ShortCode = shortCode ?? string.Empty;
        CreatedUtc = DateTimeOffset.UtcNow;
        ModifiedUtc = CreatedUtc;
    }

    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string ShortCode { get; private set; } = string.Empty;

    public string Description { get; private set; } = string.Empty;

    public Guid? RootPersonId { get; private set; }

    public string NumberingPolicyName { get; private set; } = string.Empty;

    public DateTimeOffset CreatedUtc { get; private set; }

    public DateTimeOffset ModifiedUtc { get; private set; }

    public IReadOnlyCollection<BranchMembership> Memberships => _memberships.AsReadOnly();

    public void UpdateDetails(
        string name,
        string shortCode,
        string description,
        string numberingPolicyName)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Branch name is required.", nameof(name));
        }

        Name = name;
        ShortCode = shortCode ?? string.Empty;
        Description = description ?? string.Empty;
        NumberingPolicyName = numberingPolicyName ?? string.Empty;
        ModifiedUtc = DateTimeOffset.UtcNow;
    }

    public void SetRootPerson(Guid? personId)
    {
        RootPersonId = personId;
        ModifiedUtc = DateTimeOffset.UtcNow;
    }
}
