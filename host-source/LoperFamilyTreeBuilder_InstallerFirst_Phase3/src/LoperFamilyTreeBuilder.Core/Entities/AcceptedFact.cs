namespace LoperFamilyTreeBuilder.Core.Entities;

public sealed class AcceptedFact
{
    private AcceptedFact() { }

    public AcceptedFact(Guid personId, Guid researchClaimId, Guid approvalId,
        string factType, string value, string actor)
    {
        if (string.IsNullOrWhiteSpace(factType)) throw new ArgumentException("Fact type is required.", nameof(factType));
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Fact value is required.", nameof(value));
        Id = Guid.NewGuid();
        PersonId = personId;
        ResearchClaimId = researchClaimId;
        ApprovalId = approvalId;
        FactType = factType.Trim();
        Value = value;
        AcceptedBy = string.IsNullOrWhiteSpace(actor) ? "Local Application" : actor.Trim();
        AcceptedUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid PersonId { get; private set; }
    public Guid ResearchClaimId { get; private set; }
    public Guid ApprovalId { get; private set; }
    public string FactType { get; private set; } = string.Empty;
    public string Value { get; private set; } = string.Empty;
    public string AcceptedBy { get; private set; } = string.Empty;
    public DateTimeOffset AcceptedUtc { get; private set; }
    public Person Person { get; private set; } = null!;
}
